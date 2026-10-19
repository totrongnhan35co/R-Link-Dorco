using BarcodeVerificationSystem.Controller;
using BarcodeVerificationSystem.Utils;
using BarcodeVerificationSystem.Labels.ProjectLabel;
using BarcodeVerificationSystem.Model.CaoSuDongNai;
using BarcodeVerificationSystem.Model.CaoSuDongNai.Response;
using BarcodeVerificationSystem.Model.CodeGeneration;
using BarcodeVerificationSystem.Model.Droco;
using BarcodeVerificationSystem.Model.Payload.DispatchingPayload;
using BarcodeVerificationSystem.Model.Payload.DispatchingPayload.Response;
using BarcodeVerificationSystem.Model.Payload.ManufacturingPayload.Response;
using BarcodeVerificationSystem.Model.RunningMode.Dispatching;
using BarcodeVerificationSystem.Model.THTrueMilk;
using BarcodeVerificationSystem.Model.Woka;
using CommonVariable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace BarcodeVerificationSystem.Model
{
    public class JobModel
    {
        #region Properties
        public bool IsJobOnline;
        public ResponseOrder DispatchingOrderPayload { get; set; } = null;
        public ResponseProcessOrder.Data ProcessOrderItem = null;
        public ResponseReservation Reservation;
        public ReservationItem ReservationItem;
        public ProductItem CaoSuProduct;
        public double productWeight;
        public string LOTNumber = "";
        public List<PalletModel> PalletList;
        public List<CartonModel> CartonList;
        // === Droco 4-level packaging ===
        public List<BoxModel> BoxList;
        public List<DrocoPalletModel> DrocoPalletList;
        public List<DrocoCartonModel> DrocoCartonList;  

        public int NumberOfCodesInBox { get; set; } = 0;       
        public int NumberOfBoxesInCarton { get; set; } = 0;    
        public int NumberOfCartonsInPallet { get; set; } = 0;  
        // === Droco Batch Info ===
        public string BatchNumber { get; set; } = "";
        public string SalesOrder { get; set; } = ""; 
        public string ProductModel { get; set; } = "";

        //
        public ProcessOrder ProcessOrder;
        public bool IsRePrintMode;

        public string TableName = "";
        public string BarcodeColumnName = "";
        public int TemplateIndex = 0;
    

        public bool isPushedDatabase { get; set; } = false;
        public bool IsProcessOrderMode = false;
        public bool IsReservationMode = false;

        public int SelectedRESItemIndex { get; set; } = 0;
        public int SelectedBatchIndex { get; set; } = 0;
        public int SelectedMaterialIndex { get; set; } = 0;
        public bool IsOnlineJob { get; set; } = false;

        // Sync data parameters
        public int NumberOfCodesInPallet { get; set; } = 0;

        public int NumberOfPrintedCodes { get; set; } = 0;
        public int PhanLoaiCount { get; set; } = 0;
        public int NumberOfNeededSentCodes { get; set; } = 0;
        public int NumberOfSaaSSentCodes { get; set; } = 0;
        public int NumberOfSAPSentCodes { get; set; } = 0;
        public int NumberOfCheckSaaSSentCodes { get; set; } = 0;
        public int NumberOfCheckSAPSentCodes { get; set; } = 0;
        public int FirstGeneratedCodeIndex { get; set; } = 0;
        public int LastGeneratedCodeIndex { get; set; } = 0;
        public string LastCheckedProductName { get; set; }
        public int DateCheckPassed { get; set; } = 0;
        public int DateCheckFailed { get; set; } = 0;
        // Disposal tracking per batch
        public List<BatchDisposalModel> BatchDisposalCounts { get; set; }

        // Recheck tracking per batch
        public List<BatchRecheckModel> BatchRecheckCounts { get; set; }

        private CompareType _CompareType = CompareType.CanRead;
        private string _StaticText = "";
        private string _DirectoryDatabase = "";
        private string _DirectoryDatabaseCodeSSCC = "";
        private List<PODModel> _PODFormat;
        private string _FileName = "";
        private bool _AutoLoad = true;
        private string _UserCreate = "";
        private string _DatabaseBufferPath = "";
        private string _CheckedResultPath = "";
        private string _PrintedResponePath = "";
        private bool _PrinterSeries = true;
        private string _TemplatePrint = "";
        private double _NumberTotalsCode = 0;
        private bool _IsFirstRowHeader = false;
        private bool _IsFirstRowHeaderSscc = false;
        private JobType _JobType = JobType.AfterProduction;
        private JobStatus _JobStatus = JobStatus.NewlyCreated;
        private CompleteJobStatus _CompleteJobStatus = CompleteJobStatus.Created;
        public CompareType CompareType { get => _CompareType; set => _CompareType = value; }
        public string StaticText { get => _StaticText; set => _StaticText = value; }
        public string DirectoryDatabase { get => _DirectoryDatabase; set => _DirectoryDatabase = value; }
        public string DirectoryDatabaseCodeSSCC { get => _DirectoryDatabaseCodeSSCC; set => _DirectoryDatabaseCodeSSCC = value ?? ""; }
        public List<PODModel> PODFormat { get => _PODFormat; set => _PODFormat = value; }
        public string FileName { get => _FileName; set => _FileName = value; }
        public bool AutoLoad { get => _AutoLoad; set => _AutoLoad = value; }
        public bool IsFirstRowHeader { get => _IsFirstRowHeader; set => _IsFirstRowHeader = value; }
        public bool IsFirstRowHeaderSscc { get => _IsFirstRowHeaderSscc; set => _IsFirstRowHeaderSscc = value; }
        public string UserCreate { get => _UserCreate; set => _UserCreate = value; }
        public string DatabaseBufferPath { get => _DatabaseBufferPath; set => _DatabaseBufferPath = value; }
        public string CheckedResultPath { get => _CheckedResultPath; set => _CheckedResultPath = value; }
        public string PrintedResponePath { get => _PrintedResponePath; set => _PrintedResponePath = value; }
        public string TemplatePrint { get => _TemplatePrint; set => _TemplatePrint = value; }
        public bool PrinterSeries { get => _PrinterSeries; set => _PrinterSeries = value; }
        public double NumberTotalsCode { get => _NumberTotalsCode; set => _NumberTotalsCode = value; }
        public JobType JobType { get => _JobType; set => _JobType = value; }
        public JobStatus JobStatus { get => _JobStatus; set => _JobStatus = value; }
        public CompleteJobStatus CompleteJobStatus { get => _CompleteJobStatus; set => _CompleteJobStatus = value; }
        public string LastCheckedLot { get; set; }
        public string LastCheckedProductCode { get; set; }
        public string LastCheckedPrintJob { get; set; }
        public string PrintJobProductCode { get; set; } = "";
        public string PrintJobWeightRange { get; set; } = "";
        public double EstimatedTons { get; set; }   // số tấn dự kiến
        public int Volume { get; set; }              // volume 1 SP (ml)
        public long TotalRunTimeTicks { get; set; }  // tổng thời gian vận hành (tích lũy)
        public DateTime FirstRunTime { get; set; } = DateTime.MinValue; // thời gian bắt đầu lần đầu
        public DateTime LastRunTime { get; set; } = DateTime.MinValue; // thời gian kết thúc gần nhất
        // ?? THTrueMilk job snapshot (l�u t?i th?i �i?m t?o job) ??????????????
        public THTrueMilkOperatingMode THJobOperatingMode { get; set; } = THTrueMilkOperatingMode.BatchOneQrCode;
        public int THJobDeltaMinutes { get; set; } = 0;
        public int THJobBufferCount { get; set; } = 0;
        public int THJobLogInterval { get; set; } = 5;
        public string THJobErrorImageFolder { get; set; } = "";
        public int THJobNMinutes { get; set; } = 30;
        public string THJobProductId { get; set; } = "";
        public string THJobProductGtin { get; set; } = "";
        public string THJobProductName { get; set; } = "";
        public string THJobBatchNo { get; set; } = "";
        public string THJobCameraModelForTraining { get; set; } = "";
        public string THJobIV4CameraModelForTraining { get; set; } = "";
        public int THJobMonitorInterval { get; set; } = 0;
        public int THJobQrThreshold { get; set; } = 1;
        public int THJobExpiryMonths { get; set; } = 6;
        public bool THJobAllowNsxHsdChange { get; set; } = true;
        public int THMaxConsecutiveError { get; set; } = 5;
        public string CurrentBatchQrCode { get; set; } = "";
        public DateTime CurrentBatchDate { get; set; } = DateTime.MinValue;
        public int CurrentQrId { get; set; } = -1;
        public DateTime Mode2NextRefreshAt { get; set; } = DateTime.MinValue;
        public long TotalSent { get; set; } = 0;
        public long TotalReceived { get; set; } = 0;
        public long TotalRsfpReceived { get; set; } = 0;
        public long TotalRlinkPrinted { get; set; } = 0;

        #endregion Properties

        #region Methods
        public void SaveFile()
        {
            try
            {
                // "CHECK_" ? l�u v�o PathJobsCheckApp
                bool isCheckJob = !string.IsNullOrWhiteSpace(FileName)
                                  && FileName.StartsWith("CHECK_", StringComparison.OrdinalIgnoreCase);

                string dir = isCheckJob
                    ? CommVariables.PathJobsCheckApp
                    : CommVariables.PathJobsApp;

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filePath = Path.Combine(dir, FileName + Shared.Settings.JobFileExtension);
                var xs = new XmlSerializer(typeof(JobModel));
                using (TextWriter sw = new StreamWriter(filePath))
                {
                    xs.Serialize(sw, this);
                }

                // Only save ProductCodes CSV for Droco project
                if (ProjectLabel.IsDroco)
                {
                    SaveProductCodesToCsv();
                }
            }
            catch { }
        }
        public void SaveCheckedJobFile(string fileName)
        {
            try
            {
                string dir = CommonVariable.CommVariables.PathJobsCheckApp;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filePath = Path.Combine(dir, fileName + Shared.Settings.JobFileExtension);
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(JobModel));
                using (System.IO.TextWriter sw = new System.IO.StreamWriter(filePath))
                {
                    xs.Serialize(sw, this);
                }
            }
            catch
            {
                // Optionally log error
            }
        }
        public void SaveAsCheckedFile()
        {
            try
            {
                string dir = CommVariables.PathJobsCheckApp;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filePath = Path.Combine(dir, FileName + Shared.Settings.JobFileExtension);
                var xs = new XmlSerializer(typeof(JobModel));
                using (TextWriter sw = new StreamWriter(filePath))
                {
                    xs.Serialize(sw, this);
                }
            }
            catch { }
        }
        public static JobModel LoadFile(String fileName)
        {
            try
            {
                JobModel info = null;
                var xs = new XmlSerializer(typeof(JobModel));
                using (var sr = new StreamReader(fileName))
                {
                    var xr = XmlReader.Create(sr);
                    info = (JobModel)xs.Deserialize(xr);
                }

                // Load ProductCodes from separate CSV file
                info?.LoadProductCodesFromCsv();

               // ProjectLogger.WriteInfo($"[LoadFile] SUCCESS: {fileName}");
                return info;
            }
            catch (Exception ex)
            {
                //ProjectLogger.WriteError($"[LoadFile] FAILED: {fileName} | {ex.GetType().Name}: {ex.Message}");
                return null;
            }

        }

        /// <summary>
        /// Save ProductCodes to CSV file in ProductCodesDroco directory
        /// Keeps \x1D byte as-is (no conversion)
        /// </summary>
        public void SaveProductCodesToCsv()
        {
            try
            {
                if (BoxList == null || BoxList.Count == 0) return;

                string csvDir = CommVariables.PathProductCodesDroco;
                if (!Directory.Exists(csvDir))
                    Directory.CreateDirectory(csvDir);

                string csvPath = Path.Combine(csvDir, FileName + "_codes.csv");

                using (var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true)))
                {
                    foreach (var box in BoxList)
                    {
                        if (box.ProductCodes == null || box.ProductCodes.Count == 0) continue;
                        foreach (var code in box.ProductCodes)
                        {
                            writer.WriteLine($"{box.QrCode},{code}");
                        }
                    }
                }

                ProjectLogger.WriteInfo($"[SaveProductCodesToCsv] Saved {BoxList.Sum(b => b.ProductCodes?.Count ?? 0)} codes to {csvPath}");
            }
            catch (Exception ex)
            {
                ProjectLogger.WriteError($"[SaveProductCodesToCsv] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Load ProductCodes from CSV file into BoxList
        /// Keeps \x1D byte as-is (no conversion)
        /// </summary>
        private void LoadProductCodesFromCsv()
        {
            try
            {
                if (BoxList == null || BoxList.Count == 0) return;

                string csvDir = CommVariables.PathProductCodesDroco;
                string csvPath = Path.Combine(csvDir, FileName + "_codes.csv");

                if (!File.Exists(csvPath))
                {
                    ProjectLogger.WriteInfo($"[LoadProductCodesFromCsv] No CSV file found: {csvPath}");
                    return;
                }

                foreach (var line in File.ReadAllLines(csvPath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    int commaIndex = line.IndexOf(',');
                    if (commaIndex < 0) continue;

                    string boxQrCode = line.Substring(0, commaIndex);
                    string gs1Code = line.Substring(commaIndex + 1);

                    var box = BoxList.FirstOrDefault(b => b.QrCode == boxQrCode);
                    if (box != null)
                    {
                        if (box.ProductCodes == null)
                            box.ProductCodes = new List<string>();
                        box.ProductCodes.Add(gs1Code);
                    }
                }

                int totalLoaded = BoxList.Sum(b => b.ProductCodes?.Count ?? 0);
                ProjectLogger.WriteInfo($"[LoadProductCodesFromCsv] Loaded {totalLoaded} codes from {csvPath}");
            }
            catch (Exception ex)
            {
                ProjectLogger.WriteError($"[LoadProductCodesFromCsv] Error: {ex.Message}");
            }
        }

        #endregion Methods
    }
}
