using System;

namespace RLinkMasterSimulator.Models;

/// <summary>
/// Payload cho POST /api/rlink/monitor — heartbeat định kỳ từ R-Link client.
/// </summary>
public class MonitorPayload
{
    // ── Định danh ────────────────────────────────────────────────
    public string RLinkName { get; set; } = "";
    public string LineId { get; set; } = "";
    public string? FactoryCode { get; set; }
    public string? IpAddress { get; set; }

    // ── Trạng thái thiết bị ──────────────────────────────────────
    /// <summary>idle | running | paused | error</summary>
    public string? Status { get; set; } = "idle";
    public bool IsPrinterConnected { get; set; }
    public bool IsCameraConnected { get; set; }
    public bool IsPlcConnected { get; set; }
    public bool IsDatabaseConnected { get; set; }

    public string? IpAddressPrinter { get; set; }
    public string? IpAddressCamera { get; set; }
    public string? IpAddressPlc { get; set; }

    // ── Thông tin job / batch ────────────────────────────────────
    public string? CurrentJobName { get; set; }
    public string? CurrentBatch { get; set; }

    public string? Operator { get; set; }

    // ── Tiến độ QR ───────────────────────────────────────────────
    public int TotalQrAllocated { get; set; }
    public int TotalQrUsed { get; set; }
    public int TotalQrFailed { get; set; }
    public int TotalProduced { get; set; }

    // ── Thống kê camera chi tiết ─────────────────────────────────
    public MonitorStats? Stats { get; set; }

    // ── Ảnh lỗi gần nhất ────────────────────────────────────────
    public string? LastErrorImageBase64 { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class MonitorStats
{
    public int TotalCode { get; set; }
    public int TotalChecked { get; set; }
    public int CheckPassed { get; set; }
    public int CheckFailed { get; set; }
    public int NumberPrinted { get; set; }
}