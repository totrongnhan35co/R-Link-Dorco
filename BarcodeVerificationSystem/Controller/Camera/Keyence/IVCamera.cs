using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BarcodeVerificationSystem.Controller.Camera.Keyence
{
    public class IVCamera
    {
        public class IV4ParseResult
        {
            public string FullFrame { get; set; } = "";
            public string ResultNumber { get; set; } = "";
            public string OverallResult { get; set; } = "";
        }

        public class Iv4ProgramInfo
        {
            public int ProgramNo { get; set; }
            public string Name { get; set; }
            public override string ToString() => ProgramNo.ToString("D4") + (string.IsNullOrWhiteSpace(Name) ? string.Empty : " " + Name);
        }

        public const int MaxProgramCount = 32;
        public int ProgramScanDelayMs { get; set; } = 150;

        private Socket _socket;
            private readonly byte[] _receiveBuffer = new byte[4096];
            private readonly StringBuilder _textBuffer = new StringBuilder();
            private readonly object _sync = new object();
            private CancellationTokenSource _cts;
            private bool _manualDisconnect;
            private bool _isListening;
            private TaskCompletionSource<string> _exclusiveCommandTcs;
            private bool _isExclusiveMode;

            public IVCamera(string ipAddress, int port, int timeoutMs)
            {
                IpAddress = ipAddress;
                Port = port;
                TimeoutMs = timeoutMs;
            }

            public string IpAddress { get; }

            public int Port { get; }

            public int TimeoutMs { get; }

            public string TriggerCommand { get; set; } = "T1";

            public string TriggerWithDataCommand { get; set; } = "T2";

            public string CommandTerminator { get; set; } = "\r";

            public Encoding TextEncoding { get; set; } = Encoding.ASCII;

            public bool WrapCommandWithStxEtx { get; set; }

            public bool ParseFramesWithStxEtx { get; set; }

            public bool LoadImageOnReceive { get; set; } = true;

            public string ImageFolderPath { get; set; } = @"D:\hinhanh\img";

            public int ImageWaitMs { get; set; } = 1000;

            public int ImageRetryIntervalMs { get; set; } = 100;

            public string[] ImageExtensions { get; set; } = new[] { ".png", ".bmp", ".jpg", ".jpeg" };

            public event Action<string> FrameReceived;

            public event Action<IV4ParseResult> FrameParsed;

            public event Action<string> LogReceived;

            public bool Connect()
            {
                try
                {
                    _manualDisconnect = false;
                    CleanupSocket();

                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        SendTimeout = TimeoutMs,
                        ReceiveTimeout = TimeoutMs,
                        NoDelay = true
                    };

                    SetKeepAlive(_socket, keepAliveTime: 30000, keepAliveInterval: 5000);

                    var endpoint = new IPEndPoint(IPAddress.Parse(IpAddress), Port);
                    var result = _socket.BeginConnect(endpoint, null, null);

                    bool connected = result.AsyncWaitHandle.WaitOne(TimeoutMs, true);
                    if (!connected || !_socket.Connected)
                    {
                        WriteLog(string.Format("Connect timeout sau {0}ms @ {1}:{2}", TimeoutMs, IpAddress, Port));
                        CleanupSocket();
                        return false;
                    }

                    _socket.EndConnect(result);

                    lock (_sync)
                    {
                        _textBuffer.Clear();
                    }

                    WriteLog(string.Format("Connected @ {0}:{1}", IpAddress, Port));
                    return true;
                }
                catch (Exception ex)
                {
                    WriteLog("Connect failed: " + ex.Message);
                    CleanupSocket();
                    return false;
                }
            }

            public bool IsConnected()
            {
                try
                {
                    return _socket != null && _socket.Connected;
                }
                catch
                {
                    return false;
                }
            }

            public bool StartListening()
            {
                if (!IsConnected())
                {
                    WriteLog("StartListening failed: socket chua ket noi");
                    return false;
                }

                if (_isListening)
                {
                    return true;
                }

                _cts = new CancellationTokenSource();
                _manualDisconnect = false;
                _isListening = true;
                Task.Run(() => ListenLoop(_cts.Token), _cts.Token);
                return true;
            }

            public void StopListening()
            {
                try
                {
                    _cts?.Cancel();
                }
                catch
                {
                }

                _isListening = false;
                WriteLog("StopListening called");
            }

            public void Disconnect()
            {
                _manualDisconnect = true;

                try
                {
                    _cts?.Cancel();
                }
                catch
                {
                }

                Thread.Sleep(50);
                CleanupSocket();
                WriteLog("Disconnected");
            }

            private bool PauseListeningForExclusiveCommands()
            {
                bool wasListening = _isListening;
                _isExclusiveMode = true;
                _exclusiveCommandTcs = null;
                return wasListening;
            }

            private void ResumeListeningAfterExclusiveCommands(bool wasListening)
            {
                _isExclusiveMode = false;
                _exclusiveCommandTcs = null;
            }

            public bool Trigger()
            {
                return SendCommand(TriggerCommand);
            }

            public bool TriggerWithData()
            {
                return SendCommand(TriggerWithDataCommand);
            }

            public bool EnableOutput()
            {
                return SendCommand("OE,1");
            }

            public bool SendCommand(string command)
            {
                try
                {
                    if (!IsConnected())
                    {
                        throw new InvalidOperationException("Chua ket noi IV4");
                    }

                    string payload = BuildCommandPayload(command);
                    byte[] data = TextEncoding.GetBytes(payload);
                    int sent = 0;

                    while (sent < data.Length)
                    {
                        int count = _socket.Send(data, sent, data.Length - sent, SocketFlags.None);
                        if (count <= 0)
                        {
                            throw new SocketException((int)SocketError.ConnectionReset);
                        }

                        sent += count;
                    }

                    WriteLog("Sent: " + FormatVisiblePayload(payload));
                    return true;
                }
                catch (Exception ex)
                {
                    WriteLog("SendCommand failed: " + ex.Message);
                    return false;
                }
            }

            public async Task<string> SendAndWaitAsync(
                string command,
                string expectedPrefix,
                string errorPrefix,
                CancellationToken cancellationToken,
                int timeoutMs = 5000)
            {
                if (!IsConnected())
                    throw new InvalidOperationException("Chua ket noi IV4");

                var sw = Stopwatch.StartNew();

                while (sw.ElapsedMilliseconds < timeoutMs)
                {
                    var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _exclusiveCommandTcs = tcs;

                    try
                    {
                        SendCommand(command);

                        int remaining = timeoutMs - (int)sw.ElapsedMilliseconds;
                        if (remaining <= 0) break;

                        var completedTask = await Task.WhenAny(
                            tcs.Task,
                            Task.Delay(remaining, cancellationToken)
                        ).ConfigureAwait(false);

                        if (completedTask != tcs.Task)
                        {
                            WriteLog($"SendAndWaitAsync timeout: {command}");
                            return null;
                        }

                        string response = tcs.Task.Result;

                        if (!string.IsNullOrEmpty(errorPrefix) &&
                            response.StartsWith(errorPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException($"Camera loi: {response}");
                        }

                        if (!string.IsNullOrEmpty(expectedPrefix) &&
                            !response.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            WriteLog($"SendAndWaitAsync prefix mismatch: got '{response}', expected '{expectedPrefix}', retrying...");
                            continue;
                        }

                        return response;
                    }
                    finally
                    {
                        _exclusiveCommandTcs = null;
                    }
                }

                WriteLog($"SendAndWaitAsync timeout: {command}");
                return null;
            }

            public Bitmap LoadImageById(string imageId)
            {
                if (string.IsNullOrWhiteSpace(imageId))
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(ImageFolderPath) || !Directory.Exists(ImageFolderPath))
                {
                    WriteLog("Image folder not found: " + ImageFolderPath);
                    return null;
                }

                string normalizedId = NormalizeImageId(imageId);
                var watch = Stopwatch.StartNew();

                while (watch.ElapsedMilliseconds <= ImageWaitMs)
                {
                    string filePath = FindImageFile(ImageFolderPath, normalizedId, ImageExtensions);
                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
                        try
                        {
                            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var tempImage = Image.FromStream(stream))
                            {
                                WriteLog("Load image: " + filePath);
                                return new Bitmap(tempImage);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteLog("Load image failed: " + ex.Message);
                        }
                    }

                    Thread.Sleep(Math.Max(10, ImageRetryIntervalMs));
                }

                WriteLog("Image not found: " + imageId);
                return null;
            }

            private async Task<int?> GetCurrentProgramNumberCoreAsync(CancellationToken cancellationToken)
            {
                string resp = await SendAndWaitAsync("PR", "PR,", "ER,PR", cancellationToken).ConfigureAwait(false);
                if (resp == null || resp.Length < 4)
                    return null;
                if (int.TryParse(resp.Substring(3).Trim(), out int no))
                    return no;
                return null;
            }

            public async Task ChangeProgramAsync(int programNo, CancellationToken cancellationToken = default)
            {
                if (programNo < 0 || programNo >= MaxProgramCount)
                    throw new ArgumentOutOfRangeException(nameof(programNo), "Phải trong khoảng 0–31.");

                if (!IsConnected())
                    throw new InvalidOperationException("Chua ket noi IV4.");

                await SendAndWaitAsync($"PW,{programNo:D3}", "PW", "ER,PW", cancellationToken).ConfigureAwait(false);
            }

            private void ListenLoop(CancellationToken token)
            {
                WriteLog("Listen loop started");

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (!IsConnected())
                        {
                            WriteLog("Socket mat ket noi");
                            break;
                        }

                        if (!_socket.Poll(50000, SelectMode.SelectRead))
                        {
                            Thread.Sleep(1);
                            continue;
                        }

                        int count = _socket.Receive(_receiveBuffer);
                        if (count <= 0)
                        {
                            WriteLog("Thiet bi dong ket noi");
                            break;
                        }

                        string chunk = TextEncoding.GetString(_receiveBuffer, 0, count);
                        Debug.WriteLine(chunk.Replace("\r", "\\r").Replace("\n", "\\n"));

                        lock (_sync)
                        {
                            _textBuffer.Append(chunk);
                            DispatchBufferedFrames();
                        }
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                    }
                    catch (SocketException ex)
                    {
                        WriteLog("SocketException: " + ex.SocketErrorCode + " - " + ex.Message);
                        break;
                    }
                    catch (Exception ex)
                    {
                        WriteLog("Listen error: " + ex.Message);
                        break;
                    }
                }

                _isListening = false;
                WriteLog("Listen loop ended");

                if (!token.IsCancellationRequested && !_manualDisconnect)
                {
                    Thread.Sleep(2000);
                    Reconnect();
                }
            }

            private void DispatchBufferedFrames()
            {
                if (ParseFramesWithStxEtx)
                {
                    DispatchStxEtxFrames();
                    return;
                }

                DispatchCrLfFrames();
            }

            private void DispatchCrLfFrames()
            {
                string buffer = _textBuffer.ToString();
                int startIndex = 0;

                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] != '\r' && buffer[i] != '\n')
                    {
                        continue;
                    }

                    if (i > startIndex)
                    {
                        string frame = buffer.Substring(startIndex, i - startIndex).Trim();
                        if (!string.IsNullOrWhiteSpace(frame) &&
                            !string.Equals(frame, TriggerCommand, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_isExclusiveMode && _exclusiveCommandTcs != null && !_exclusiveCommandTcs.Task.IsCompleted)
                            {
                                _exclusiveCommandTcs.TrySetResult(frame);
                            }
                            else if (frame.StartsWith("RT,"))
                            {
                                string[] parts = frame.Split(',');
                                var parsed = new IV4ParseResult
                                {
                                    FullFrame = frame,
                                    ResultNumber = parts.Length > 1 ? parts[1] : "",
                                    OverallResult = parts.Length > 2 ? parts[2] : ""
                                };
                                FrameParsed?.Invoke(parsed);
                            }
                            else
                            {
                                FrameReceived?.Invoke(frame);
                            }
                        }
                    }

                    startIndex = i + 1;
                }

                _textBuffer.Clear();
                if (startIndex < buffer.Length)
                {
                    _textBuffer.Append(buffer, startIndex, buffer.Length - startIndex);
                }
            }

            private void DispatchStxEtxFrames()
            {
                string buffer = _textBuffer.ToString();
                int searchIndex = 0;
                int keepFromIndex = buffer.Length;

                while (searchIndex < buffer.Length)
                {
                    int stxIndex = buffer.IndexOf('\x02', searchIndex);
                    if (stxIndex < 0)
                    {
                        keepFromIndex = buffer.Length;
                        break;
                    }

                    int etxIndex = buffer.IndexOf('\x03', stxIndex + 1);
                    if (etxIndex < 0)
                    {
                        keepFromIndex = stxIndex;
                        break;
                    }

                    string frame = buffer.Substring(stxIndex + 1, etxIndex - stxIndex - 1).Trim();
                    if (!string.IsNullOrWhiteSpace(frame) &&
                        !string.Equals(frame, TriggerCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        if (_isExclusiveMode && _exclusiveCommandTcs != null && !_exclusiveCommandTcs.Task.IsCompleted)
                        {
                            _exclusiveCommandTcs.TrySetResult(frame);
                        }
                        else
                        {
                            FrameReceived?.Invoke(frame);
                        }
                    }

                    searchIndex = etxIndex + 1;
                    keepFromIndex = searchIndex;
                }

                _textBuffer.Clear();
                if (keepFromIndex >= 0 && keepFromIndex < buffer.Length)
                {
                    _textBuffer.Append(buffer, keepFromIndex, buffer.Length - keepFromIndex);
                }
            }

            private void Reconnect()
            {
                CleanupSocket();

                if (Connect())
                {
                    StartListening();
                    WriteLog("Reconnected successfully");
                }
                else
                {
                    WriteLog("Reconnect failed");
                }
            }

            private void CleanupSocket()
            {
                try
                {
                    if (_socket != null)
                    {
                        if (_socket.Connected)
                        {
                            _socket.Shutdown(SocketShutdown.Both);
                        }

                        _socket.Close();
                        _socket.Dispose();
                    }
                }
                catch
                {
                }

                _socket = null;
                _isListening = false;

                lock (_sync)
                {
                    _textBuffer.Clear();
                }
            }

            private static string NormalizeImageId(string imageId)
            {
                string value = (imageId ?? string.Empty).Trim().Replace('\\', '/');
                int lastSlash = value.LastIndexOf('/');
                if (lastSlash >= 0 && lastSlash < value.Length - 1)
                {
                    value = value.Substring(lastSlash + 1);
                }

                return value;
            }

            private static string FindImageFile(string basePath, string imageId, string[] extensions)
            {
                if (string.IsNullOrWhiteSpace(basePath) || string.IsNullOrWhiteSpace(imageId))
                {
                    return null;
                }

                string[] validExtensions = extensions ?? new[] { ".png", ".bmp", ".jpg", ".jpeg" };
                foreach (string candidate in BuildImageIdCandidates(imageId))
                {
                    string exactPath = Path.Combine(basePath, candidate);
                    if (File.Exists(exactPath))
                    {
                        return exactPath;
                    }

                    string matchedFile = Directory.EnumerateFiles(basePath, "*" + candidate + "*.*", SearchOption.AllDirectories)
                        .FirstOrDefault(file => validExtensions.Any(ext =>
                            string.Equals(Path.GetExtension(file), ext, StringComparison.OrdinalIgnoreCase)));

                    if (!string.IsNullOrWhiteSpace(matchedFile))
                    {
                        return matchedFile;
                    }
                }

                return null;
            }

            private static IEnumerable<string> BuildImageIdCandidates(string imageId)
            {
                string normalized = NormalizeImageId(imageId);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    yield return normalized;
                }

                string digitsOnly = new string((normalized ?? string.Empty).Where(char.IsDigit).ToArray());
                if (string.IsNullOrWhiteSpace(digitsOnly))
                {
                    yield break;
                }

                string trimmed = digitsOnly.TrimStart('0');
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    trimmed = "0";
                }

                foreach (string candidate in new[]
                {
                trimmed,
                long.Parse(trimmed).ToString("D5"),
                long.Parse(trimmed).ToString("D6"),
                long.Parse(trimmed).ToString("D7")
            }.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return candidate;
                    }
                }
            }

            private void WriteLog(string message)
            {
                LogReceived?.Invoke(message);
            }

            private string BuildCommandPayload(string command)
            {
                string payload = command ?? string.Empty;

                if (WrapCommandWithStxEtx)
                {
                    payload = "\x02" + payload + "\x03";
                }

                return payload + (CommandTerminator ?? string.Empty);
            }

            private static string FormatVisiblePayload(string payload)
            {
                return (payload ?? string.Empty)
                    .Replace("\x02", "<STX>")
                    .Replace("\x03", "<ETX>")
                    .Replace("\r", "<CR>")
                    .Replace("\n", "<LF>");
            }

            private static void SetKeepAlive(Socket socket, uint keepAliveTime, uint keepAliveInterval)
            {
                try
                {
                    uint valueSize = (uint)Marshal.SizeOf(typeof(uint));
                    byte[] keepAliveValues = new byte[valueSize * 3];

                    BitConverter.GetBytes(1u).CopyTo(keepAliveValues, 0);
                    BitConverter.GetBytes(keepAliveTime).CopyTo(keepAliveValues, (int)valueSize);
                    BitConverter.GetBytes(keepAliveInterval).CopyTo(keepAliveValues, (int)(valueSize * 2));

                    socket.IOControl(IOControlCode.KeepAliveValues, keepAliveValues, new byte[keepAliveValues.Length]);
                }
                catch
                {
                }
            }

            public async Task<IReadOnlyList<Iv4ProgramInfo>> GetProgramListAsync(
                CancellationToken cancellationToken = default(CancellationToken))
            {
                var result = new List<Iv4ProgramInfo>();
                if (!IsConnected())
                {
                    WriteLog("Chua ket noi.");
                    return result;
                }

                bool wasListening = PauseListeningForExclusiveCommands();
                int? originalProgram = null;

                try
                {
                    originalProgram = await GetCurrentProgramNumberCoreAsync(cancellationToken).ConfigureAwait(false);
                    WriteLog("Original program: " + (originalProgram.HasValue ? originalProgram.Value.ToString("D3") : "null"));

                    for (int i = 0; i < MaxProgramCount; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            WriteLog("Scan program " + i.ToString("D3"));

                            var pwResponse = await SendAndWaitAsync("PW," + i.ToString("D3"), "PW", "ER,PW", cancellationToken).ConfigureAwait(false);

                            if (pwResponse == null)
                            {
                                WriteLog("PW timeout tai program " + i.ToString("D3"));
                                continue;
                            }

                            if (ProgramScanDelayMs > 0)
                            {
                                await Task.Delay(ProgramScanDelayMs, cancellationToken).ConfigureAwait(false);
                            }

                            string pnrLine = await SendAndWaitAsync("PNR", "PNR,", "ER,PNR", cancellationToken).ConfigureAwait(false);

                            string name = pnrLine != null && pnrLine.Length > 4
                                ? pnrLine.Substring(4).Replace("\0", string.Empty).Trim()
                                : string.Empty;

                            WriteLog("Program " + i.ToString("D3") + " = [" + name + "]");

                            result.Add(new Iv4ProgramInfo
                            {
                                ProgramNo = i,
                                Name = name
                            });
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            WriteLog("Scan loi tai program " + i.ToString("D3") + ": " + ex.Message);
                        }
                    }
                }
                finally
                {
                    if (originalProgram.HasValue &&
                        originalProgram.Value >= 0 &&
                        originalProgram.Value < MaxProgramCount &&
                        IsConnected())
                    {
                        try
                        {
                            await SendAndWaitAsync(
                                "PW," + originalProgram.Value.ToString("D3"),
                                "PW",
                                "ER,PW",
                                CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            WriteLog("Khong khoi phuc duoc program goc: " + ex.Message);
                        }
                    }

                    ResumeListeningAfterExclusiveCommands(wasListening);
                }

                return result;
            }
    }
}
