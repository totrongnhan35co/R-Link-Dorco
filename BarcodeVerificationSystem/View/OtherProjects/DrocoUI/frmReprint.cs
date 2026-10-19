using BarcodeVerificationSystem.Controller;
using BarcodeVerificationSystem.Model;
using BarcodeVerificationSystem.View.CustomDialogs;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using static BarcodeVerificationSystem.Controller.Shared;

namespace BarcodeVerificationSystem.View.OtherProjects.DrocoUI
{
    public partial class frmReprint : Form
    {
        private readonly JobModel _jobModel;

        public JobModel CurrentJob { get; set; }
        private BindingList<ReprintItem> _reprintList = new BindingList<ReprintItem>();
        private BindingList<ReprintItem> _reprintQrList = new BindingList<ReprintItem>();
        private BindingList<ReprintItem> _reprintBoxQrList = new BindingList<ReprintItem>();
        private BindingList<ReprintItem> _reprintCartonQrList = new BindingList<ReprintItem>();
        private BindingList<ReprintItem> _reprintPalletQrList = new BindingList<ReprintItem>();
        private ManualResetEvent _printResponseEvent = new ManualResetEvent(false);

        private readonly List<string[]> _printedCodeList;
        
        // Selected code info for reprint
        private string _selectedQrCode = "";
        private string _selectedQrType = "";

        private volatile bool _lastPrintSuccess = false;
        public frmReprint(JobModel jobModel, List<string[]> printedCodeList)
        {

            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            _jobModel = jobModel;
            _printedCodeList = printedCodeList;
         
            // Bind data sources for 3 grids
            dgvListBoxQrReprint.DataSource = _reprintBoxQrList;
            dgvListCartonQrReprint.DataSource = _reprintCartonQrList;
            dgvListPalletQrReprint.DataSource = _reprintPalletQrList;
            
            // Clear selection after data binding completes
            dgvListBoxQrReprint.DataBindingComplete += (s, e) => 
            {
                dgvListBoxQrReprint.ClearSelection();
                dgvListBoxQrReprint.CurrentCell = null;
            };
            dgvListCartonQrReprint.DataBindingComplete += (s, e) => 
            {
                dgvListCartonQrReprint.ClearSelection();
                dgvListCartonQrReprint.CurrentCell = null;
            };
            dgvListPalletQrReprint.DataBindingComplete += (s, e) => 
            {
                dgvListPalletQrReprint.ClearSelection();
                dgvListPalletQrReprint.CurrentCell = null;
            };
            
            // Search textbox
            txtQrReprint.TextChanged += txtQrReprint_TextChanged;
            
            Shared.OnSerialDeviceReadDataChange += Shared_OnSerialDeviceReadDataChange;
            Shared.OnPrinterDataChange += OnPrinterDataChange;
       
            SetColumnWidths(dgvListBoxQrReprint);
            SetColumnWidths(dgvListCartonQrReprint);
            SetColumnWidths(dgvListPalletQrReprint);
            
            // Load QR codes from job model
            LoadQrCodesToGrids();
            LoadReprintListFromFile();
        }
        private void OnPrinterDataChange(object sender, EventArgs e)
        {
            // Kiểm tra phản hồi từ máy in thường (ForProduct)
            if (sender is PODDataModel podDataModel)
            {
                Debug.WriteLine("Printer response: " + podDataModel.Text);
                // phản hồi thành công là "RYES" hoặc "OK"
                if (podDataModel.Text.Contains("RYES") || podDataModel.Text.Contains("OK"))
                {
                    _lastPrintSuccess = true;
                    _printResponseEvent.Set();
                }
                else
                {
                    _lastPrintSuccess = false;
                    _printResponseEvent.Set();
                }
            }
        }
        private void SetColumnWidths(DataGridView dgv)
        {
            // Đảm bảo DataGridView đã có cột
            if (dgv.Columns.Count == 0) return;

            if (dgv.Columns.Contains("CodeValue"))
                dgv.Columns["CodeValue"].Width = 600;
            if (dgv.Columns.Contains("Status"))
                dgv.Columns["Status"].Width = 150;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

        }
        
        /// <summary>
        /// Load QR codes from job model into 3 grids
        /// </summary>
        private void LoadQrCodesToGrids()
        {
            // Box QR codes
            if (_jobModel.BoxList != null)
            {
                foreach (var box in _jobModel.BoxList)
                {
                    _reprintBoxQrList.Add(new ReprintItem
                    {
                        CodeValue = box.QrCode,
                        Status = box.IsSent ? "Đã xác nhận" : "Chưa xác nhận"
                    });
                }
            }
            
            // Carton QR codes
            if (_jobModel.DrocoCartonList != null)
            {
                foreach (var carton in _jobModel.DrocoCartonList)
                {
                    _reprintCartonQrList.Add(new ReprintItem
                    {
                        CodeValue = carton.QrCode,
                        Status = carton.IsSent ? "Đã xác nhận" : "Chưa xác nhận"
                    });
                }
            }
            
            // Pallet QR codes
            if (_jobModel.DrocoPalletList != null)
            {
                foreach (var pallet in _jobModel.DrocoPalletList)
                {
                    _reprintPalletQrList.Add(new ReprintItem
                    {
                        CodeValue = pallet.QrCode,
                        Status = pallet.IsSent ? "Đã xác nhận" : "Chưa xác nhận"
                    });
                }
            }
        }
        
        /// <summary>
        /// Search/filter when text changes in txtQrReprint
        /// </summary>
        private void txtQrReprint_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtQrReprint.Text.Trim().ToLower();
            
            // Box grid
            dgvListBoxQrReprint.DataSource = null;
            if (string.IsNullOrEmpty(searchText))
            {
                dgvListBoxQrReprint.DataSource = _reprintBoxQrList;
            }
            else
            {
                var filtered = new BindingList<ReprintItem>(
                    _reprintBoxQrList.Where(x => 
                        x.CodeValue.ToLower().Contains(searchText)).ToList());
                dgvListBoxQrReprint.DataSource = filtered;
            }
            
            // Carton grid
            dgvListCartonQrReprint.DataSource = null;
            if (string.IsNullOrEmpty(searchText))
            {
                dgvListCartonQrReprint.DataSource = _reprintCartonQrList;
            }
            else
            {
                var filtered = new BindingList<ReprintItem>(
                    _reprintCartonQrList.Where(x => 
                        x.CodeValue.ToLower().Contains(searchText)).ToList());
                dgvListCartonQrReprint.DataSource = filtered;
            }
            
            // Pallet grid
            dgvListPalletQrReprint.DataSource = null;
            if (string.IsNullOrEmpty(searchText))
            {
                dgvListPalletQrReprint.DataSource = _reprintPalletQrList;
            }
            else
            {
                var filtered = new BindingList<ReprintItem>(
                    _reprintPalletQrList.Where(x => 
                        x.CodeValue.ToLower().Contains(searchText)).ToList());
                dgvListPalletQrReprint.DataSource = filtered;
            }
            
            // Clear selected code
            _selectedQrCode = "";
            _selectedQrType = "";
        }
        
        /// <summary>
        /// CellClick for Box QR grid
        /// </summary>
        private void dgvListBoxQrReprint_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dgvListBoxQrReprint.Rows.Count)
            {
                _selectedQrCode = dgvListBoxQrReprint.Rows[e.RowIndex].Cells["CodeValue"].Value?.ToString() ?? "";
                _selectedQrType = "QR Hộp";
                
                // Clear selection on other grids
                dgvListCartonQrReprint.ClearSelection();
                dgvListPalletQrReprint.ClearSelection();
            }
        }
        
        /// <summary>
        /// CellClick for Carton QR grid
        /// </summary>
        private void dgvListCartonQrReprint_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dgvListCartonQrReprint.Rows.Count)
            {
                _selectedQrCode = dgvListCartonQrReprint.Rows[e.RowIndex].Cells["CodeValue"].Value?.ToString() ?? "";
                _selectedQrType = "QR Thùng";
                
                // Clear selection on other grids
                dgvListBoxQrReprint.ClearSelection();
                dgvListPalletQrReprint.ClearSelection();
            }
        }
        
        /// <summary>
        /// CellClick for Pallet QR grid
        /// </summary>
        private void dgvListPalletQrReprint_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dgvListPalletQrReprint.Rows.Count)
            {
                _selectedQrCode = dgvListPalletQrReprint.Rows[e.RowIndex].Cells["CodeValue"].Value?.ToString() ?? "";
                _selectedQrType = "QR Pallet";
                
                // Clear selection on other grids
                dgvListBoxQrReprint.ClearSelection();
                dgvListCartonQrReprint.ClearSelection();
            }
        }
        
        /// <summary>
        /// ColumnHeaderMouseClick - Clear selection when clicking header
        /// </summary>
        private void dgv_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null)
            {
                dgv.ClearSelection();
                dgv.CurrentCell = null;
            }
            
            // Clear selected code
            _selectedQrCode = "";
            _selectedQrType = "";
        }
        
        /// <summary>
        /// CellMouseDown - Block selection when clicking header (Row index = -1)
        /// </summary>
        private void dgv_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Nếu click vào header (Row index = -1) → chặn selection
            if (e.RowIndex == -1)
            {
                var dgv = sender as DataGridView;
                if (dgv != null)
                {
                    dgv.ClearSelection();
                    dgv.CurrentCell = null;
                }
                
                // Clear selected code
                _selectedQrCode = "";
                _selectedQrType = "";
            }
        }
        
        /// <summary>
        /// btnSendQrRePrint_Click - Reprint selected QR code
        /// </summary>
        private void btnSendQrRePrint_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedQrCode))
            {
                CustomMessageBox.Show("Vui lòng chọn mã QR cần in lại!", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Xác định loại từ _selectedQrType
            QRType qrType;
            switch (_selectedQrType)
            {
                case "QR Thùng": qrType = QRType.Carton; break;
                case "QR Pallet": qrType = QRType.Pallet; break;
                default: qrType = QRType.Box; break;
            }
            
            // Confirm
            DialogResult confirm = CustomMessageBox.Show(
                $"Bạn có muốn in lại mã {_selectedQrType}:\n{_selectedQrCode}?",
                "Xác nhận in lại", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
            
            // Reprint
            if (Shared.IsZebraPrinterReady())
            {
                Shared.PrintZebraDroco(_selectedQrCode, qrType);
                
                CustomMessageBox.Show($"In lại thành công: {_selectedQrCode}", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                WriteReprintLog($"Đã in lại mã: {_selectedQrCode} | Loại: {_selectedQrType}");
            }
            else
            {
                CustomMessageBox.Show("Máy in Zebra chưa kết nối!", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// btnResetValueQr_Click - Clear search and selection
        /// </summary>
        private void btnResetValueQr_Click(object sender, EventArgs e)
        {
            txtQrReprint.Text = "";
            _selectedQrCode = "";
            _selectedQrType = "";
            
            // Clear selection on all grids
            dgvListBoxQrReprint.ClearSelection();
            dgvListBoxQrReprint.CurrentCell = null;
            dgvListCartonQrReprint.ClearSelection();
            dgvListCartonQrReprint.CurrentCell = null;
            dgvListPalletQrReprint.ClearSelection();
            dgvListPalletQrReprint.CurrentCell = null;
        }
        
        /// <summary>
        /// Normalize GS1 separator về dạng chuẩn \F để so sánh nhất quán.
        /// </summary>
        private static string NormalizeCode(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // ── [FIX] Strip GS1 AI parentheses: (00)046071... → 00046071... ──────
            var match = System.Text.RegularExpressions.Regex.Match(input, @"^\((\d{2,4})\)(.+)$");
            if (match.Success)
                input = match.Groups[1].Value + match.Groups[2].Value;
            // ─────────────────────────────────────────────────────────────────────

            return input;
        }

        /// <summary>
        /// Nhận diện loại mã QR dựa vào dữ liệu thực từ job (BoxList / DrocoCartonList / DrocoPalletList).
        /// Thay thế cách cũ dùng prefix số (không tương thích với mã Excel SSCC).
        /// </summary>
        private void DetectCodeType(string code, string normalizedCode,
            out string type, out bool isGS1, out bool isQR)
        {
            type = "Không xác định";
            isGS1 = false;
            isQR = false;

            // --- Ưu tiên: kiểm tra QR Hộp ---
            bool isBox = _jobModel.BoxList?.Any(b =>
                string.Equals(b.QrCode, code, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(b.QrCode, normalizedCode, StringComparison.OrdinalIgnoreCase)) == true;
            if (isBox) { type = "QR Hộp"; isQR = true; return; }

            // --- Kiểm tra QR Thùng ---
            bool isCarton = _jobModel.DrocoCartonList?.Any(c =>
                string.Equals(c.QrCode, code, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.QrCode, normalizedCode, StringComparison.OrdinalIgnoreCase)) == true;
            if (isCarton) { type = "QR Thùng"; isQR = true; return; }

            // --- Kiểm tra QR Pallet ---
            bool isPallet = _jobModel.DrocoPalletList?.Any(p =>
                string.Equals(p.QrCode, code, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.QrCode, normalizedCode, StringComparison.OrdinalIgnoreCase)) == true;
            if (isPallet) { type = "QR Pallet"; isQR = true; return; }

            // --- Kiểm tra GS1 (mã sản phẩm trong ProductCodes của hộp) ---
            bool isProductCode = _jobModel.BoxList?.Any(b =>
                b.ProductCodes != null &&
                b.ProductCodes.Any(pc =>
                    string.Equals(pc, code, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(NormalizeCode(pc), normalizedCode, StringComparison.OrdinalIgnoreCase))) == true;
            if (isProductCode) { type = "GS1"; isGS1 = true; }
        }
        private void Shared_OnSerialDeviceReadDataChange(object sender, EventArgs e)
        {
            if (IsDisposed || !Visible || !IsHandleCreated) return;
            if (!(sender is DetectModel detectModel)) return;

            string code = detectModel.Text?.Trim();
            if (string.IsNullOrEmpty(code)) return;

            // Hiển thị giá trị quét được vào txtQrReprint
            Invoke(new Action(() =>
            {
                txtQrReprint.Text = code;
            }));

            /*
            string normalizedCode = NormalizeCode(code);

            // Nhận diện loại mã theo cấu trúc job thực tế
            string type;
            bool isGS1, isQR;
            DetectCodeType(code, normalizedCode, out type, out isGS1, out isQR);

            bool isQrTab = false;
            Invoke(new Action(() => isQrTab = tabRePrint.SelectedTab == tabRePrintQr));

            // ── Tab QR: in lại Hộp / Thùng / Pallet ─────────────────────────────
            if (isQrTab)
            {
                if (!isQR)
                {
                    Invoke(new Action(() =>
                        CustomMessageBox.Show(
                            $"Mã vừa quét không phải QR Hộp/Thùng/Pallet hoặc không thuộc job này!\nLoại nhận diện: {type}",
                            "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                    return;
                }

                DialogResult confirm = DialogResult.No;
                Invoke(new Action(() =>
                {
                    confirm = CustomMessageBox.Show(
                        $"Bạn có muốn in lại mã {type}:\n{code}",
                        "Xác nhận in lại", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                }));
                if (confirm != DialogResult.Yes) return;
                if (Shared.IsZebraPrinterReady())
                // if (Shared.Settings.ZebraPrinter.PODController.IsConnected())
                {
                    QRType qrType;
                    switch (type)
                    {
                        case "QR Thùng": qrType = QRType.Carton; break;
                        case "QR Pallet": qrType = QRType.Pallet; break;
                        default: qrType = QRType.Box; break;
                    }

                    // ── [FIX] Dùng normalizedCode (đã strip `()`) thay vì code gốc ──
                    Shared.PrintZebraDroco(normalizedCode, qrType);
                    WriteReprintLog($"Đã in lại mã: {normalizedCode} | Loại: {type}");

                    Invoke(new Action(() =>
                    {
                        _reprintQrList.Insert(0, new ReprintItem
                        {
                            CodeValue = code,
                            Status = "Đã gửi in lại"
                        });
                        SaveReprintListToFile();
                        DesignUI.CuzAlert.CuzAlert.Show(
                            $"In lại thành công: {code}",
                            DesignUI.CuzAlert.Alert.enmType.Success,
                            new Size(500, 120), new Point(Location.X, Location.Y), this.Size);
                    }));
                }
                else
                {
                    WriteReprintLog($"Lỗi khi in lại mã: {code} | Loại: {type} | Zebra chưa kết nối");
                    Invoke(new Action(() =>
                    {
                        _reprintQrList.Insert(0, new ReprintItem
                        {
                            CodeValue = code,
                            Status = "Lỗi máy in"
                        });
                        CustomMessageBox.Show("Máy in Zebra chưa kết nối!", "Cảnh báo",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }));
                }
                return;
            }

            // ── Tab GS1: in lại mã sản phẩm ─────────────────────────────────────
            if (!isGS1) return;

            string codeCopy = code;
            string normalizedCopy = normalizedCode;
            string typeCopy = type;

            Task.Run(() =>
            {
                bool printSuccess = false;
                var productPrinter = Shared.Settings.PrinterList
                    .FirstOrDefault(p => p.RoleOfPrinter == RoleOfStation.ForProduct);

                if (productPrinter?.PODController != null && productPrinter.PODController.IsConnected())
                {
                    // Tìm dòng dữ liệu trong PrintedCodeList, normalize trước khi so sánh
                    string[] dataRow = _printedCodeList?.FirstOrDefault(row =>
                        row.Skip(2).Any(field =>
                            string.Equals(field, codeCopy, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(NormalizeCode(field), normalizedCopy, StringComparison.OrdinalIgnoreCase)));

                    if (dataRow != null)
                    {
                        string command = "DATA;";
                        var arrSkip = dataRow.Skip(2).ToArray();

                        if (Shared.Settings.PrintFieldForVerifyAndPrint.Count() == 0)
                        {
                            command += string.Join(Shared.Settings.SplitCharacter.ToString(),
                                arrSkip.Select(x => x ?? Shared.Settings.FailedDataSentToPrinter));
                        }
                        else
                        {
                            command += string.Join(Shared.Settings.SplitCharacter.ToString(),
                                Shared.Settings.PrintFieldForVerifyAndPrint
                                    .Where(x => x.Index < arrSkip.Length + 1)
                                    .Select(x => arrSkip[x.Index - 1] ?? Shared.Settings.FailedDataSentToPrinter));
                        }

                        _printResponseEvent.Reset();
                        _lastPrintSuccess = false;
                        productPrinter.PODController.Send(command);
                        printSuccess = _printResponseEvent.WaitOne(5000) && _lastPrintSuccess;
                    }
                    else
                    {
                        WriteReprintLog($"Không tìm thấy dữ liệu trong database cho mã: {codeCopy}");
                    }
                }

                WriteReprintLog(printSuccess
                    ? $"Đã in lại mã: {codeCopy} | Loại: {typeCopy}"
                    : $"Lỗi khi in lại mã: {codeCopy} | Loại: {typeCopy}");

                if (IsDisposed) return;
                Invoke(new Action(() =>
                {
                    _reprintQrList.Insert(0, new ReprintItem
                    {
                        CodeValue = codeCopy,
                        Status = printSuccess ? "Đã gửi in lại" : "Lỗi máy in"
                    });
                    SaveReprintListToFile();

                    if (printSuccess)
                    {
                        DesignUI.CuzAlert.CuzAlert.Show(
                            $"In lại thành công: {codeCopy}",
                            DesignUI.CuzAlert.Alert.enmType.Success,
                            new Size(500, 120), new Point(Location.X, Location.Y), this.Size);
                    }
                    else
                    {
                        CustomMessageBox.Show(
                            $"In lại thất bại hoặc máy in không phản hồi!\nMã: {codeCopy}",
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }));
            });
            */
        }
        private void LoadReprintListFromFile()
        {
            try
            {
                string baseDirectory = @"C:\ProgramData\R-Link";
                string logDirectory = System.IO.Path.Combine(baseDirectory, "Reprint");
                string jobName = _jobModel?.FileName ?? "UnknownJob";
                //string fileGs1 = System.IO.Path.Combine(logDirectory, $"ReprintList_GS1_{jobName}.txt");
                string fileQr = System.IO.Path.Combine(logDirectory, $"ReprintList_QR_{jobName}.txt");

                // Load GS1
                //if (System.IO.File.Exists(fileGs1))
                //{
                //    var lines = System.IO.File.ReadAllLines(fileGs1, Encoding.UTF8);
                //    foreach (var line in lines)
                //    {
                //        var parts = line.Split('|');
                //        if (parts.Length == 4)
                //        {
                //            _reprintList.Add(new ReprintItem
                //            {
                //                CodeType = parts[0],
                //                CodeValue = parts[1],
                //                Time = parts[2],
                //                Status = parts[3]
                //            });
                //        }
                //    }
                //}

                // Load QR
                if (System.IO.File.Exists(fileQr))
                {
                    var lines = System.IO.File.ReadAllLines(fileQr, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 2)
                        {
                            _reprintQrList.Add(new ReprintItem
                            {
                                CodeValue = parts[0],
                                Status = parts[1]
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LoadReprintListFromFile failed: " + ex.Message);
            }
        }
        private void SaveReprintListToFile()
        {
            try
            {
                string baseDirectory = @"C:\ProgramData\R-Link";
                string logDirectory = System.IO.Path.Combine(baseDirectory, "Reprint");
                if (!System.IO.Directory.Exists(logDirectory))
                    System.IO.Directory.CreateDirectory(logDirectory);

                string jobName = _jobModel?.FileName ?? "UnknownJob";
                // Lưu mã QR
                string fileQr = System.IO.Path.Combine(logDirectory, $"ReprintList_QR_{jobName}.txt");
                var linesQr = _reprintQrList.Select(x => $"{x.CodeValue}|{x.Status}");
                System.IO.File.WriteAllLines(fileQr, linesQr, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SaveReprintListToFile failed: " + ex.Message);
            }
        }
        private void WriteReprintLog(string message)
        {
            try
            {
                // 1. Xác định thư mục log
                string baseDirectory = @"C:\ProgramData\R-Link";
                string logDirectory = System.IO.Path.Combine(baseDirectory, "Reprint");

                // 2. Tạo thư mục nếu chưa tồn tại
                if (!System.IO.Directory.Exists(logDirectory))
                {
                    System.IO.Directory.CreateDirectory(logDirectory);
                }

                // 3. Đặt tên file log theo ngày
                string fileName = $"Reprint_{DateTime.Now:yyyy-MM-dd}.txt";
                string filePath = System.IO.Path.Combine(logDirectory, fileName);

                // 4. Ghi log với timestamp chi tiết
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                System.IO.File.AppendAllText(filePath, logEntry, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WriteReprintLog failed: " + ex.Message);
            }
        }
    }

    // Model cho DataGridView
    public class ReprintItem
    {
        public string CodeValue { get; set; }
        public string Status { get; set; }
    }
}
