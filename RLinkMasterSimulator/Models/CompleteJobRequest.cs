using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLinkMasterSimulator.Models;

public class CompleteJobRequest
{
    public string RLinkName { get; set; } = "";
    public string JobName { get; set; } = "";
    public int QrUsedCount { get; set; }
    /// <summary>Tổng số QR đã cấp phát cho job (= buffer size). QrRemaining = QrTotalAllocated - QrUsedCount.</summary>
    public int QrTotalAllocated { get; set; }
    public int ProducedCount { get; set; }
    /// <summary>1=BatchOneQrCode, 2=AutoRefreshByTime, 3=ProductOneQrCode</summary>
    public int OperatingMode { get; set; } = 3;
    /// <summary>Số QR trong buffer (dùng cho Mode 2).</summary>
    public int BufferCount { get; set; } = 1;
    /// <summary>GTIN / mã sản phẩm — dùng để sinh QR GS1 URL đúng định dạng.</summary>
    public string ProductId { get; set; } = "";
    /// <summary>Line ID — gán vào cột line_id khi lưu QR.</summary>
    public string LineId { get; set; } = "";
    /// <summary>Batch number — gán vào cột batch khi lưu QR.</summary>
    public string BatchNo { get; set; } = "";
    /// <summary>Số QR còn is_used=FALSE trong PG — server dùng để quyết định cấp thêm.</summary>
    public int QrRemainingInDb { get; set; } = 0;

    /// <summary>Thống kê chi tiết theo QR code.</summary>
    public List<QrStatsItem> StatsArr { get; set; } = new List<QrStatsItem>();
}

/// <summary>Thống kê kiểm tra cho 1 QR code.</summary>
public class QrStatsItem
{
    public string QrCode { get; set; }
    public QrStatsDetail Stats { get; set; } = new QrStatsDetail();
}

/// <summary>Chi tiết thống kê kiểm tra.</summary>
public class QrStatsDetail
{
    public int TotalCheck { get; set; }
    public int TotalQRCheckPassed { get; set; }
    public int TotalQRCheckFailed { get; set; }
    public int TotalDateCheckPassed { get; set; }
    public int TotalDateCheckFailed { get; set; }
    public int TotalA { get; set; }
    public int TotalB { get; set; }
    public int TotalF { get; set; }
}
