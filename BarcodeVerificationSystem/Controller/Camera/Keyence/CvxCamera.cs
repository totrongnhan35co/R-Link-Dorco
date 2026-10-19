using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BarcodeVerificationSystem.Model;
using BarcodeVerificationSystem.Controller.Camera.Keyence;
using FluentFTP;

namespace BarcodeVerificationSystem.Controller.Camera.Keyence
{
    public class CvxCamera
    {
        private Socket client;
        private Socket listener;
        private byte[] bytes = new byte[4096];
        private NetworkStream commandStream;
        public string ip;
        public int port;
        private int timeout;
        private ProtocolMode _protocol = ProtocolMode.Line;
        private CancellationTokenSource _cts;
        private readonly StringBuilder _rxBuffer = new StringBuilder();
        private readonly byte[] _recvBuffer = new byte[8192];
        private AsyncFtpClient _ftpClient;
        private TaskCompletionSource<string> _commandResponseTcs;
        private int _frameDispatchCount = 0;

        public string FtpImagePath { get; set; }
        public event Action<string> OnCameraFrameReceived;
        public event Action<int> CurrentProgramChanged;

        public CvxCamera(string IpAddr, int PortNum, int Timeout)
        {
            ip = IpAddr;
            port = PortNum;
            timeout = Timeout;
        }

        public void SetProtocolMode(ProtocolMode mode) => _protocol = mode;

        // ══════════════════════════════════════════
        // CONNECT
        // ══════════════════════════════════════════
        public bool Connect()
        {
            try
            {
                CleanupSocket();

                // Command socket
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.ReceiveTimeout = timeout;
                client.NoDelay = true;
                SetKeepAlive(client, 10000, 2000, 3);
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ip), port);
                var result = client.BeginConnect(remoteEP, null, null);
                bool connected = result.AsyncWaitHandle.WaitOne(timeout, true);
                if (!connected || !client.Connected)
                {
                    Console.WriteLine($"[CVX] ❌ Connect timeout {timeout}ms @ {ip}:{port}");
                    CleanupSocket();
                    return false;
                }
                client.EndConnect(result);
                commandStream = new NetworkStream(client, ownsSocket: false);

                // Listener socket
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.ReceiveTimeout = timeout;
                listener.NoDelay = true;
                SetKeepAlive(listener, 10000, 2000, 3);
                listener.Connect(remoteEP);

                Console.WriteLine($"[CVX] ✅ Connected @ {ip}:{port}");

                // FTP connection
                Console.WriteLine($"[CVX FTP] Connecting to {ip}:21...");
                _ftpClient = new AsyncFtpClient(ip, 21);
                _ftpClient.Config.ConnectTimeout = timeout;
                _ftpClient.Config.ReadTimeout = timeout;
                _ftpClient.Config.DataConnectionConnectTimeout = timeout;
                _ftpClient.Config.DataConnectionReadTimeout = timeout;
                _ftpClient.Config.SelfConnectMode = FtpSelfConnectMode.Always;
                try
                {
                    _ftpClient.Connect();
                    Console.WriteLine($"[CVX FTP] ✅ FTP connected @ {ip}:21");
                }
                catch (Exception ftpEx)
                {
                    Console.WriteLine($"[CVX FTP] ⚠️ FTP connect failed: {ftpEx.Message}");
                    _ftpClient = null;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CVX] ❌ Connect failed: {ex.Message}");
                CleanupSocket();
                try { _ftpClient?.Dispose(); } catch { }
                _ftpClient = null;
                return false;
            }
        }

        public bool IsConnected() => client?.Connected == true && listener?.Connected == true;

        // ══════════════════════════════════════════
        // START LISTENING
        // ══════════════════════════════════════════
        public bool StartListening()
        {
            if (listener == null || !listener.Connected)
            {
                Console.WriteLine("[CVX] StartListening fail: socket not connected");
                return false;
            }

            _cts = new CancellationTokenSource();

            if (_protocol == ProtocolMode.Framed)
            {
                Task.Run(() => FramedListenLoop(_cts.Token), _cts.Token);
            }
            else
            {
                Task.Run(() => LineListenLoop(_cts.Token), _cts.Token);
            }
            return true;
        }

        public void StopListening()
        {
            try { _cts?.Cancel(); } catch { }
            Console.WriteLine("[CVX] StopListening called");
        }

        // ══════════════════════════════════════════
        // LINE MODE (raw text, comma+CR split)
        // ══════════════════════════════════════════
        private void LineListenLoop(CancellationToken token)
        {
            Console.WriteLine($"[CVX] LineListenLoop started @ {DateTime.Now}");
            const int POLL_US = 50_000;
            bool dataReceivedRecently = false;

            while (!token.IsCancellationRequested)
            {
                if (listener == null || !listener.Connected)
                {
                    Console.WriteLine("[CVX] Socket disconnected");
                    break;
                }

                try
                {
                    if (listener.Poll(POLL_US, SelectMode.SelectRead))
                    {
                        int bytesRec = listener.Receive(bytes);
                        if (bytesRec > 0)
                        {
                            string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                            Debug.WriteLine($"[CVX] RX raw: {data}");

                            char[] separator = { ',', '\r' };
                            string[] parts = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var part in parts)
                            {
                                string trimmedPart = part.Trim();
                                if (!string.IsNullOrEmpty(trimmedPart))
                                {
                                    var detectModel = new DetectModel
                                    {
                                        Text = trimmedPart,
                                    };
                                    Shared.RaiseOnCameraReadDataChangeEvent(detectModel);
                                    dataReceivedRecently = true;
                                }
                            }
                            _cts?.Token.WaitHandle.WaitOne(1);
                        }
                        else
                        {
                            Console.WriteLine("[CVX] No data, connection closed");
                            break;
                        }
                    }
                    else
                    {
                        _cts?.Token.WaitHandle.WaitOne(dataReceivedRecently ? 1 : 10);
                        dataReceivedRecently = false;
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"[CVX] SocketException: {ex.SocketErrorCode} - {ex.Message}");
                    if (ex.SocketErrorCode == SocketError.ConnectionAborted) break;
                    _cts?.Token.WaitHandle.WaitOne(1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CVX] Exception: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine($"[CVX] LineListenLoop ended @ {DateTime.Now}");
            if (!token.IsCancellationRequested)
            {
                Console.WriteLine("[CVX] Mất kết nối → reconnect sau 2s...");
                _cts?.Token.WaitHandle.WaitOne(2000);
                Reconnect();
            }
        }

        // ══════════════════════════════════════════
        // FRAMED MODE (CR delimiter)
        // ══════════════════════════════════════════
        private void FramedListenLoop(CancellationToken token)
        {
            Console.WriteLine($"[CVX] FramedListenLoop (CR) started @ {DateTime.Now}");
            const int POLL_US = 50_000;
            bool dataReceivedRecently = false;

            while (!token.IsCancellationRequested)
            {
                if (listener == null || !listener.Connected)
                {
                    Console.WriteLine("[CVX] Socket disconnected");
                    break;
                }

                try
                {
                    if (listener.Poll(POLL_US, SelectMode.SelectRead))
                    {
                        int bytesRec = listener.Receive(bytes);
                        if (bytesRec > 0)
                        {
                            string chunk = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                            Debug.WriteLine($"[CVX] RX raw: {chunk.Replace("\r", "\\r").Replace("\n", "\\n")}");

                            _rxBuffer.Append(chunk);
                            ProcessBufferedFrames();
                            dataReceivedRecently = true;
                            _cts?.Token.WaitHandle.WaitOne(1);
                        }
                        else
                        {
                            Console.WriteLine("[CVX] No data, connection closed");
                            break;
                        }
                    }
                    else
                    {
                        _cts?.Token.WaitHandle.WaitOne(dataReceivedRecently ? 1 : 10);
                        dataReceivedRecently = false;
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"[CVX] SocketException: {ex.SocketErrorCode} - {ex.Message}");
                    if (ex.SocketErrorCode == SocketError.ConnectionAborted) break;
                    _cts?.Token.WaitHandle.WaitOne(1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CVX] Exception: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine($"[CVX] FramedListenLoop ended @ {DateTime.Now}");
            if (!token.IsCancellationRequested)
            {
                Console.WriteLine("[CVX] Mất kết nối → reconnect sau 2s...");
                _cts?.Token.WaitHandle.WaitOne(2000);
                Reconnect();
            }
        }

        private void ProcessBufferedFrames()
        {
            string buf = _rxBuffer.ToString();
            int start = 0;

            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i] != '\r' && buf[i] != '\n') continue;
                if (i > start)
                {
                    string frame = buf.Substring(start, i - start).Trim();
                    if (!string.IsNullOrEmpty(frame))
                        DispatchFrame(frame);
                }
                start = i + 1;
            }

            _rxBuffer.Clear();
            if (start < buf.Length)
                _rxBuffer.Append(buf, start, buf.Length - start);
        }

        // ══════════════════════════════════════════
        // DISPATCH FRAME — parse QR,QR_Result,NSX,Time,HSD,Batch,OCR_Result,Image_ID
        // ══════════════════════════════════════════
        private void DispatchFrame(string frame)
        {
            // Skip command responses (RUN, ER, PL, PR)
            string upperFrame = frame.Trim().ToUpper();
            if (upperFrame.StartsWith("RUN") || upperFrame.StartsWith("ER")
                || upperFrame == "PL" || upperFrame.StartsWith("PR,"))
            {
                Console.WriteLine($"[CVX] Command response: '{frame}'");
                _commandResponseTcs?.TrySetResult(frame.Trim());
                return;
            }

            Console.WriteLine($"[CVX] DispatchFrame #{++_frameDispatchCount}: '{frame}'");

            // Remove TA prefix if present
            string cleanedFrame = frame.StartsWith("TA", StringComparison.OrdinalIgnoreCase)
                ? frame.Substring(2).TrimStart(',').Trim()
                : frame;

            string[] parts = cleanedFrame.Split(',');

            // Format: QR,QR_Result,NSX,Time,HSD,Batch,OCR_Result,Image_ID
            string qr = parts.Length > 0 ? parts[0].Trim() : "";
            string qrResult = parts.Length > 1 ? parts[1].Trim() : "";
            string nsx = parts.Length > 2 ? parts[2].Trim() : "";
            string time = parts.Length > 3 ? parts[3].Trim() : "";
            string hsd = parts.Length > 4 ? parts[4].Trim() : "";
            string batch = parts.Length > 5 ? parts[5].Trim() : "";
            string ocrResult = parts.Length > 6 ? parts[6].Trim() : "";
            string imageId = parts.Length > 7 ? parts[7].Trim() : "";

            bool qrPass = IsResultOk(qrResult);
            bool ocrPass = IsResultOk(ocrResult);

            var detect = new DetectModel
            {
                Text = qr,
                Image = null,
                CompareResult = Shared.Settings.THCheckQrCode
                    ? (qrPass && ocrPass ? ComparisonResult.Valid : ComparisonResult.Invalided)
                    : (ocrPass ? ComparisonResult.Valid : ComparisonResult.Invalided),
            };
            detect.ExtraFields["QR_RESULT"] = qrPass ? "OK" : "NG";
            detect.ExtraFields["OCR_RESULT"] = ocrPass ? "OK" : "NG";
            detect.ExtraFields["OCR_STATUS"] = ocrPass ? "OK" : "NG";
            detect.ExtraFields["NSX"] = StripPrefix(nsx, "NSX:");
            detect.ExtraFields["TIME"] = time;
            detect.ExtraFields["HSD"] = StripPrefix(hsd, "HSD:");
            detect.ExtraFields["BATCH"] = batch;
            detect.ExtraFields["RAW_FRAME"] = cleanedFrame;
            detect.ExtraFields["RECEIVE_TIME"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            if (!string.IsNullOrEmpty(imageId))
                detect.ExtraFields["IMAGE_ID"] = imageId;

            Console.WriteLine($"[CVX] Frame: QR='{qr}' QR_Result={qrPass} OCR_Result={ocrPass} ImageID='{imageId}'");
            OnCameraFrameReceived?.Invoke(cleanedFrame);
            Shared.RaiseOnCameraReadDataChangeEvent(detect);
        }

        private static bool IsResultOk(string val)
        {
            return val.Equals("OK", StringComparison.OrdinalIgnoreCase)
                || val == "1"
                || val.Equals("PASS", StringComparison.OrdinalIgnoreCase)
                || val.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
        }

        private static string StripPrefix(string value, string prefix)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return value.Substring(prefix.Length).Trim();
            return value.Trim();
        }

        // ══════════════════════════════════════════
        // FTP — Download error image
        // ══════════════════════════════════════════
        private Bitmap DownloadErrorImage(string imageNumber)
        {
            try
            {
                string basePath = FtpImagePath;
                if (string.IsNullOrEmpty(basePath))
                    basePath = Shared.Settings?.THFtpImagePath
                        ?? @"D:\hinhanh\VS\Camera\Images";
                if (!Directory.Exists(basePath)) return null;

                string[] extensions = { ".png", ".bmp", ".jpg" };
                var result = FindImageFile(basePath, imageNumber, extensions);
                if (result != null)
                {
                    Console.WriteLine($"[CVX] Loaded: {result}");
                    using (var fs = new FileStream(result, FileMode.Open, FileAccess.Read, FileShare.Read))
                        return new Bitmap(fs);
                }
                // Retry once after 500ms
                Thread.Sleep(500);
                result = FindImageFile(basePath, imageNumber, extensions);
                if (result != null)
                {
                    Console.WriteLine($"[CVX] Loaded (retry): {result}");
                    using (var fs = new FileStream(result, FileMode.Open, FileAccess.Read, FileShare.Read))
                        return new Bitmap(fs);
                }
                Console.WriteLine($"[CVX] No image '{imageNumber}' found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CVX] Load error image failed: {ex.Message}");
                return null;
            }
        }

        private static string FindImageFile(string basePath, string imageNumber, string[] extensions)
        {
            string pattern = $"*{imageNumber}*.*";
            return Directory.EnumerateFiles(basePath, pattern, SearchOption.AllDirectories)
                .FirstOrDefault(f =>
                {
                    string ext = Path.GetExtension(f);
                    return ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
                        || ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
                        || ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase);
                });
        }

        // ══════════════════════════════════════════
        // SEND COMMAND
        // ══════════════════════════════════════════
        public bool SendCommand(string command)
        {
            try
            {
                if (client == null || !client.Connected || commandStream == null)
                    return false;

                byte[] byteData = Encoding.ASCII.GetBytes(command + "\r");
                commandStream.Write(byteData, 0, byteData.Length);
                commandStream.Flush();
                Console.WriteLine($"[CVX] Sent: {command} @ {DateTime.Now}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CVX] SendCommand error: {ex.Message}");
                return false;
            }
        }

        public bool SendRawCommandFramed(string command)
        {
            if (listener == null || !listener.Connected)
            {
                Console.WriteLine("[CVX] SendRawCommandFramed fail: socket not connected");
                return false;
            }

            byte[] byteData = Encoding.ASCII.GetBytes(command + "\r");
            listener.Send(byteData);
            Console.WriteLine($"[CVX] Sent framed: {command}");
            return true;
        }

        public bool TriggerFramed()
        {
            return SendRawCommandFramed("TA");
        }

        // ══════════════════════════════════════════
        // CHANGE PROGRAM (Framed mode)
        // ══════════════════════════════════════════
        public async Task<(bool success, string message)> ChangeProgramAsync(int programNo, CancellationToken cancellationToken = default)
        {
            if (_protocol == ProtocolMode.Framed)
            {
                // Send RUN first (camera may be in SETUP)
                SendRawCommandFramed("RUN");
                _commandResponseTcs = new TaskCompletionSource<string>();
                await Task.WhenAny(
                    _commandResponseTcs.Task,
                    Task.Delay(2000, cancellationToken)
                ).ConfigureAwait(false);

                // Send PL,1,{programNo}
                _commandResponseTcs = new TaskCompletionSource<string>();
                string plCmd = $"PL,1,{programNo:D4}";
                SendRawCommandFramed(plCmd);

                var plCompleted = await Task.WhenAny(
                    _commandResponseTcs.Task,
                    Task.Delay(10000, cancellationToken)
                ).ConfigureAwait(false);

                if (plCompleted != _commandResponseTcs.Task)
                    return (false, "Camera không phản hồi lệnh PL sau 10 giây");

                string plResponse = _commandResponseTcs.Task.Result;
                string plNormalized = plResponse?.Replace("\x02", "").Replace("\x03", "").Trim();

                if (plNormalized?.StartsWith("ER", StringComparison.OrdinalIgnoreCase) == true)
                    return (false, $"Camera lỗi: {plNormalized}");

                if (!string.Equals(plNormalized, "PL", StringComparison.OrdinalIgnoreCase))
                    return (false, $"Camera phản hồi không xác định: {plNormalized}");

                // Verify with PR
                _commandResponseTcs = new TaskCompletionSource<string>();
                SendRawCommandFramed("PR");
                var prCompleted = await Task.WhenAny(
                    _commandResponseTcs.Task,
                    Task.Delay(10000, cancellationToken)
                ).ConfigureAwait(false);

                if (prCompleted != _commandResponseTcs.Task)
                    return (true, $"Đã đổi sang program {programNo:D4} (PL ok, PR timeout)");

                string prResponse = _commandResponseTcs.Task.Result;
                string prNormalized = prResponse?.Replace("\x02", "").Replace("\x03", "").Trim();

                if (prNormalized != null && prNormalized.StartsWith("PR,1,", StringComparison.OrdinalIgnoreCase))
                {
                    string noStr = prNormalized.Substring(5);
                    if (int.TryParse(noStr, out int confirmedNo))
                    {
                        if (confirmedNo == programNo)
                            return (true, $"Đã đổi sang program {programNo:D4} thành công");
                        else
                            return (false, $"Camera trả về program {confirmedNo:D4}, yêu cầu {programNo:D4}");
                    }
                }

                return (true, $"Đã đổi sang program {programNo:D4} (PL ok, PR: {prNormalized})");
            }
            else
            {
                SendCommand("RUN");
                Thread.Sleep(100);
                if (!SendCommand($"PN{programNo:D4}"))
                    return (false, "Gửi lệnh thất bại");
                return (true, $"Đã đổi sang program {programNo:D4} (line mode)");
            }
        }

        public int QueryCurrentProgramNo()
        {
            if (_protocol == ProtocolMode.Framed)
            {
                _commandResponseTcs = new TaskCompletionSource<string>();
                SendRawCommandFramed("PR");
                var completed = Task.WhenAny(
                    _commandResponseTcs.Task,
                    Task.Delay(3000)
                ).GetAwaiter().GetResult();

                if (completed != _commandResponseTcs.Task)
                    return -1;

                string response = _commandResponseTcs.Task.Result;
                if (string.IsNullOrWhiteSpace(response))
                    return -1;

                string[] parts = response.Split(',');
                for (int i = parts.Length - 1; i >= 1; i--)
                {
                    string token = (parts[i] ?? string.Empty).Trim();
                    if (int.TryParse(token, out int no))
                    {
                        CurrentProgramChanged?.Invoke(no);
                        return no;
                    }
                }
            }
            return -1;
        }

        // ══════════════════════════════════════════
        // RECONNECT / DISCONNECT
        // ══════════════════════════════════════════
        private void Reconnect()
        {
            Disconnect();
            if (Connect()) StartListening();
            else Console.WriteLine("[CVX] ❌ Reconnect thất bại");
        }

        public void Disconnect()
        {
            try { _cts?.Cancel(); } catch { }
            Thread.Sleep(100);
            CleanupSocket();
            try
            {
                if (_ftpClient != null)
                {
                    _ftpClient.Disconnect();
                    _ftpClient.Dispose();
                }
            }
            catch { }
            _ftpClient = null;
        }

        private void CleanupSocket()
        {
            try
            {
                commandStream?.Dispose();
                if (client?.Connected == true) client.Shutdown(SocketShutdown.Both);
                client?.Dispose();
            }
            catch { }
            try
            {
                if (listener?.Connected == true) listener.Shutdown(SocketShutdown.Both);
                listener?.Dispose();
            }
            catch { }
            commandStream = null;
            client = null;
            listener = null;
            _rxBuffer.Clear();
        }

        // ══════════════════════════════════════════
        // KEEP-ALIVE
        // ══════════════════════════════════════════
        private void SetKeepAlive(Socket socket, uint keepAliveTime, uint keepAliveInterval, int keepAliveProbeCount)
        {
            try
            {
                uint size = (uint)Marshal.SizeOf(typeof(uint));
                byte[] inOptionValues = new byte[size * 3];
                byte[] outOptionValues = new byte[inOptionValues.Length];

                BitConverter.GetBytes(1u).CopyTo(inOptionValues, 0);
                BitConverter.GetBytes(keepAliveTime).CopyTo(inOptionValues, (int)size);
                BitConverter.GetBytes(keepAliveInterval).CopyTo(inOptionValues, (int)(size * 2));

                socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, outOptionValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CVX] KeepAlive fail: {ex.Message}");
            }
        }
    }

    public enum CVXResponseCode
    {
        OK,
        Timeout,
        InvalidData,
        NetworkError,
        UnknownError
    }
}
