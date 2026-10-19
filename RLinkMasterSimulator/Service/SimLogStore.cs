using RLinkMasterSimulator.Models;

namespace RLinkMasterSimulator.Services;

/// <summary>Lưu log server-side theo kiểu ring-buffer (tối đa 500 entries).</summary>
public class SimLogStore
{
    private const int MaxEntries = 500;

    // ── Generic HTTP log (middleware + LogController) ──────────────────
    private readonly LinkedList<SimLogEntry> _entries = new();
    private readonly object _lock = new();

    public void Add(string level, string message, string? source = null)
    {
        var entry = new SimLogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Source = source ?? "Server",
            Message = message
        };
        lock (_lock)
        {
            _entries.AddFirst(entry);
            while (_entries.Count > MaxEntries)
                _entries.RemoveLast();
        }
    }

    public List<SimLogEntry> GetLatest(int count = 100)
    {
        lock (_lock)
            return _entries.Take(Math.Min(count, MaxEntries)).ToList();
    }

    public void Clear()
    {
        lock (_lock) _entries.Clear();
    }

    // ── Typed: Camera ──────────────────────────────────────────────────
    private readonly LinkedList<LogCameraPayload> _camera = new();
    private readonly object _camLock = new();

    public void AddCamera(LogCameraPayload p)
    {
        lock (_camLock)
        {
            _camera.AddFirst(p);
            while (_camera.Count > MaxEntries) _camera.RemoveLast();
        }
        Add("INFO",
            $"[Camera] ← Line={p.line_id} | RLink={p.rlink_name} | Job={p.job_name} | Batch={p.batch} | " +
            $"Status={p.status ?? "-"} | OK={p.camera_ok} Fail={p.camera_fail} Total={p.total_check}",
            "Camera");
    }

    public IEnumerable<LogCameraPayload> GetCamera()
    {
        lock (_camLock) return _camera.ToList();
    }

    // ── Typed: Print ───────────────────────────────────────────────────
    private readonly LinkedList<LogPrintPayload> _print = new();
    private readonly object _printLock = new();

    public void AddPrint(LogPrintPayload p)
    {
        lock (_printLock)
        {
            _print.AddFirst(p);
            while (_print.Count > MaxEntries) _print.RemoveLast();
        }
        Add("INFO",
            $"[Print] ← Line={p.line_id} | Job={p.job_name ?? "-"} | Batch={p.batch ?? "-"} | " +
            $"Status={p.status ?? "-"} | QrUsed={p.qr_used} Produced={p.produced}",
            "Print");
    }

    public IEnumerable<LogPrintPayload> GetPrint()
    {
        lock (_printLock) return _print.ToList();
    }

    // ── Typed: Status ──────────────────────────────────────────────────
    private readonly LinkedList<LogStatusPayload> _status = new();
    private readonly object _statusLock = new();

    public void AddStatus(LogStatusPayload p)
    {
        lock (_statusLock)
        {
            _status.AddFirst(p);
            while (_status.Count > MaxEntries) _status.RemoveLast();
        }
        Add("INFO",
            $"[Status] ← Line={p.line_id} | RLink={p.rlink_name ?? "-"} | Job={p.job_name ?? "-"} | " +
            $"Batch={p.batch ?? "-"} | Status={p.status ?? "-"} | QrUsed={p.qr_used} Produced={p.produced}",
            "Status");
    }

    public IEnumerable<LogStatusPayload> GetStatus()
    {
        lock (_statusLock) return _status.ToList();
    }

    // ── Typed: CameraError ─────────────────────────────────────────────
    private readonly LinkedList<LogCameraErrorPayload> _cameraError = new();
    private readonly object _camErrLock = new();

    public void AddCameraError(LogCameraErrorPayload p)
    {
        lock (_camErrLock)
        {
            _cameraError.AddFirst(p);
            while (_cameraError.Count > MaxEntries) _cameraError.RemoveLast();
        }
        Add("WARN",
            $"[CameraError] ← Line={p.line_id} | RLink={p.rlink_name} | Type={p.error_type} | " +
            $"QR={p.qr_code} | Expected={p.expected_code} | Msg={p.error_message}",
            "CameraError");
    }

    public IEnumerable<LogCameraErrorPayload> GetCameraError()
    {
        lock (_camErrLock) return _cameraError.ToList();
    }

    // ── Sync Reports ───────────────────────────────────────────────────
    private readonly Dictionary<string, SyncStatusReport> _syncReports = new();
    private readonly object _syncLock = new();

    public void UpdateSyncReport(SyncStatusReport report)
    {
        lock (_syncLock)
            _syncReports[report.line_id] = report;
    }

    public IEnumerable<SyncStatusReport> GetSyncReports()
    {
        lock (_syncLock) return _syncReports.Values.ToList();
    }

    // ── Resync Requests ────────────────────────────────────────────────
    private readonly HashSet<string> _resyncRequests = new();
    private readonly object _resyncLock = new();

    public void RequestResync(string lineId)
    {
        lock (_resyncLock)
            _resyncRequests.Add(lineId);
        Add("INFO", $"[Resync] Requested for line={lineId}", "Resync");
    }

    /// <summary>Trả về true nếu có yêu cầu resync cho line này (hoặc "all"), đồng thời xóa yêu cầu đó.</summary>
    public bool ConsumeResyncRequest(string lineId)
    {
        lock (_resyncLock)
        {
            if (_resyncRequests.Contains("all"))
            {
                _resyncRequests.Remove("all");
                return true;
            }
            return _resyncRequests.Remove(lineId);
        }
    }
}

public class SimLogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "INFO";
    public string Source { get; set; } = "Server";
    public string Message { get; set; } = "";
}