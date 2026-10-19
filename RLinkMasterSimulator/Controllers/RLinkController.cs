using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLinkMasterSimulator.Models;
using RLinkMasterSimulator.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

[ApiController]
public class RLinkController : ControllerBase
{
    private readonly SimRLinkConfig _config;
    private readonly SimMonitorLog _monitorLog;
    private readonly SimLineStore _lineStore;
    private readonly SimLogStore _logStore;

    public RLinkController(SimRLinkConfig config, SimMonitorLog monitorLog, SimLineStore lineStore, SimLogStore logStore)
    {
        _config = config;
        _monitorLog = monitorLog;
        _lineStore = lineStore;
        _logStore = logStore;
    }

    // ── GET /api/health ──────────────────────────────────────────────
    [HttpGet("api/rlink/health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { success = true, status = "ok", timestamp = DateTime.Now });

    // ── GET /api/rlink/heartbeat ──────────────────────────────────────
    [HttpGet("api/rlink/heartbeat")]
    [AllowAnonymous]
    public IActionResult Heartbeat([FromQuery] string lineId, [FromQuery] string factoryCode)
    {
        _logStore.Add("INFO",
            $"[Heartbeat] ← lineId={lineId ?? "?"} | factory={factoryCode ?? "?"}", "RLink");
        return Ok(new { success = true, timestamp = DateTime.Now });
    }

    // ── GET /api/rlink/lines ─────────────────────────────────────────
    [HttpGet("api/rlink/lines")]
    [Authorize]
    public IActionResult GetLines([FromQuery] string factory_code = null)
    {
        var lines = string.IsNullOrWhiteSpace(factory_code)
            ? _lineStore.GetAll()
            : _lineStore.GetByFactory(factory_code);
        _logStore.Add("INFO", $"GET /api/rlink/lines factory_code={factory_code ?? "*"} → {lines.Count} lines", "RLink");
        return Ok(new
        {
            success = true,
            message = $"Lấy danh sách line thành công. Tổng: {lines.Count}.",
            total = lines.Count,
            data = lines
        });
    }


    // ── POST /api/rlink/lines/status ─────────────────────────────────
    [HttpPost("api/rlink/lines/status")]
    [Authorize]
    public IActionResult SetLineStatus([FromBody] LineStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.line_id))
            return BadRequest(new { success = false, message = "line_id là bắt buộc.", error = "line_id là bắt buộc." });

        var (ok, reason) = _lineStore.SetActive(
            request.line_id,
            request.factory_code ?? "",
            request.line_ip ?? "");

        if (!ok)
        {
            _logStore.Add("WARN", $"SetActive REJECTED line={request.line_id}: {reason}", "RLink");
            return Conflict(new { success = false, message = reason, error = reason });
        }

        _logStore.Add("INFO", $"Line ACTIVE ← [{request.factory_code}] {request.line_id} ip={request.line_ip}", "RLink");
        return Ok(new { success = true, message = $"Line {request.line_id} đã được kích hoạt.", line_id = request.line_id, line_ip = request.line_ip });
    }

    // ── POST /api/rlink/lines/deactivate ─────────────────────────────
    [HttpPost("api/rlink/lines/deactivate")]
    [Authorize]
    public IActionResult DeactivateLine([FromBody] LineStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.line_id))
            return BadRequest(new { success = false, message = "line_id là bắt buộc.", error = "line_id là bắt buộc." });

        var (ok, reason) = _lineStore.Deactivate(request.line_id, request.factory_code ?? "");

        if (!ok)
        {
            _logStore.Add("WARN", $"Deactivate REJECTED line={request.line_id}: {reason}", "RLink");
            return NotFound(new { success = false, message = reason, error = reason });
        }

        _logStore.Add("INFO", $"Line DEACTIVATED ← [{request.factory_code}] {request.line_id}", "RLink");
        return Ok(new { success = true, message = $"Line {request.line_id} đã được deactivate.", line_id = request.line_id });
    }

    // ── GET /api/rlink/settings/{line_id} ────────────────────────────
    [HttpGet("api/rlink/settings/{line_id}")]
    [Authorize]
    public IActionResult GetSettings(string line_id)
    {
        _logStore.Add("INFO", $"GET settings ← line_id={line_id}", "RLink");
        return Ok(new
        {
            success = true,
            message = "Lấy cài đặt thành công.",
            data = _config.Settings
        });
    }

    // ── GET /api/rlink/products ──────────────────────────────────────
    [HttpGet("api/rlink/products")]
    [Authorize]
    public IActionResult GetProducts()
    {
        var products = _config.Products ?? new List<ProductItem>();
        _logStore.Add("INFO", $"GET products → {products.Count} items", "RLink");
        return Ok(new
        {
            success = true,
            message = $"Lấy danh sách sản phẩm thành công. Tổng: {products.Count}.",
            total = products.Count,
            data = products
        });
    }

  

    // ── POST /api/rlink/monitor ──────────────────────────────────────
    [HttpPost("api/rlink/monitor")]
    [Authorize]
    public IActionResult Monitor([FromBody] MonitorPayload payload)
    {
        _monitorLog.Add(payload);
        _logStore.Add("INFO",
            $"[Monitor] ← {payload.RLinkName} | Line={payload.LineId} | Produced={payload.TotalProduced}",
            "Monitor");
        return Ok(new { success = true, message = "Heartbeat đã được ghi nhận." });
    }

    // ── POST /api/rlink/log/camera ───────────────────────────────────
    [HttpPost("api/rlink/log/camera")]
    [AllowAnonymous]
    public IActionResult LogCamera([FromBody] LogCameraPayload payload)
    {
        payload.timestamp = DateTime.Now;
        _logStore.AddCamera(payload);
        return Ok(new { success = true, message = "Log camera đã được ghi nhận." });
    }

   

    // ── POST /api/rlink/log/status ───────────────────────────────────
    [HttpPost("api/rlink/log/status")]
    [AllowAnonymous]
    public IActionResult LogStatus([FromBody] LogStatusPayload payload)
    {
        payload.timestamp = DateTime.Now;
        _logStore.AddStatus(payload);
        return Ok(new { success = true, message = "Log status đã được ghi nhận." });
    }

    // ── POST /api/rlink/log/camera/error ─────────────────────────────
    [HttpPost("api/rlink/log/camera/error")]
    [AllowAnonymous]
    public IActionResult LogCameraError([FromBody] LogCameraErrorPayload payload)
    {
        payload.timestamp = DateTime.Now;
        _logStore.AddCameraError(payload);
        return Ok(new { success = true, message = "Log camera error đã được ghi nhận." });
    }

    //// ── POST /api/rlink/log/error-image ─────────────────────────────
    //[HttpPost("api/rlink/log/error-image")]
    //[AllowAnonymous]
    //public IActionResult LogErrorImage([FromBody] ErrorImagePayload payload)
    //{
    //    _logStore.Add("WARN",
    //        $"[ErrorImage] ← Line={payload.line_id} QR={payload.qr_code}",
    //        "RLink");
    //    return Ok(new { success = true, message = "Ảnh lỗi đã được ghi nhận." });
    //}

    // ── GET /api/rlink/log/recent ────────────────────────────────────
    [HttpGet("api/rlink/log/recent")]
    [AllowAnonymous]
    public IActionResult GetRecentLogs()
    {
        return Ok(new
        {
            success = true,
            message = "Lấy log thành công.",
            status = _logStore.GetStatus().Take(100),
            camera = _logStore.GetCamera().Take(100),
            camera_error = _logStore.GetCameraError().Take(100),
            print = _logStore.GetPrint().Take(100),
        });
    }

    // ── POST /api/rlink/log/sync-report ──────────────────────────────
    [HttpPost("api/rlink/log/sync-report")]
    [AllowAnonymous]
    public IActionResult SyncReport([FromBody] SyncReportRequest report)
    {
        var stored = new SyncStatusReport
        {
            line_id = report.line_id ?? "",
            rlink_name = report.rlink_name ?? "",
            pending_in = report.pending_in,
            pending_camera = report.pending_camera,
            pending_error = report.pending_error,
            reported_at = DateTime.Now,
            job_details = report.job_details?.Select(j => new SyncJobDetailItem
            {
                job_name = j.job_name ?? "",
                batch = j.batch ?? "",
                pending_in = j.pending_in,
                pending_camera = j.pending_camera,
                pending_error = j.pending_error
            }).ToList() ?? new List<SyncJobDetailItem>()
        };
        _logStore.UpdateSyncReport(stored);
        return Ok(new { success = true, message = "Sync report đã được cập nhật." });
    }

    // ── GET /api/rlink/log/sync-status ───────────────────────────────
    [HttpGet("api/rlink/log/sync-status")]
    [AllowAnonymous]
    public IActionResult GetSyncStatus()
    {
        return Ok(new
        {
            success = true,
            message = "Lấy sync status thành công.",
            reports = _logStore.GetSyncReports(),
            server = new
            {
                received_in = _logStore.GetStatus().Count(),
                received_camera = _logStore.GetCamera().Count(),
                received_error = _logStore.GetCameraError().Count(),
            }
        });
    }

    // ── POST /api/rlink/log/resync-request ───────────────────────────
    [HttpPost("api/rlink/log/resync-request")]
    [AllowAnonymous]
    public IActionResult ResyncRequest([FromQuery] string line_id = "all")
    {
        _logStore.RequestResync(line_id);
        return Ok(new { success = true, message = $"Yêu cầu resync đã được gửi cho line: {line_id}.", line_id });
    }

    // ── GET /api/rlink/log/check-resync ──────────────────────────────
    [HttpGet("api/rlink/log/check-resync")]
    [AllowAnonymous]
    public IActionResult CheckResync([FromQuery] string line_id)
    {
        bool requested = _logStore.ConsumeResyncRequest(line_id ?? "");
        return Ok(new
        {
            success = true,
            message = requested ? "Có yêu cầu resync đang chờ." : "Không có yêu cầu resync.",
            resync_requested = requested
        });
    }

    // ── Helper: push danh sách QR về client qua QrBank HTTP ─────────
    private async Task PushQrCodesToClientAsync(
        List<string> qrCodes, string lineIp,
        string lineId, string lineName,
        string factoryCode, string batch)
    {
        const string QrBankUser = "qrbank";
        const string QrBankPassword = "qrbank@#062026";
        const string QrBankSecret = "NUzfz%sqL%j&0sTMfi2oYPWnTlV8wYW6TJcIssOclIVTQpG";

        string baseUrl = $"http://{lineIp}:5002";

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

            // ── 1. Login lấy token ────────────────────────────────────
            var loginBody = JsonSerializer.Serialize(new
            {
                username = QrBankUser,
                password = QrBankPassword,
                secret_key = QrBankSecret
            });
            var loginRes = await http.PostAsync(
                $"{baseUrl}/api/qrbank/login",
                new StringContent(loginBody, Encoding.UTF8, "application/json"));

            if (!loginRes.IsSuccessStatusCode)
            {
                _logStore.Add("ERROR",
                    $"[PushQr] Login thất bại → {lineIp} HTTP {(int)loginRes.StatusCode}",
                    "RLink");
                return;
            }

            var loginJson = await loginRes.Content.ReadAsStringAsync();
            var loginDoc = JsonDocument.Parse(loginJson).RootElement;
            string token = loginDoc.GetProperty("access_token").GetString() ?? "";

            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // ── 2. Push từng QR ───────────────────────────────────────
            int pushed = 0, failed = 0;
            foreach (var qr in qrCodes)
            {
                if (string.IsNullOrWhiteSpace(qr)) continue;
                var dataBody = JsonSerializer.Serialize(new
                {
                    qr_code = qr,
                    line_id = lineId,
                    line_name = lineName,
                    factory_code = factoryCode,
                    batch = batch
                });
                try
                {
                    var res = await http.PostAsync(
                        $"{baseUrl}/api/qrbank/data",
                        new StringContent(dataBody, Encoding.UTF8, "application/json"));
                    if (res.IsSuccessStatusCode) pushed++;
                    else failed++;
                }
                catch { failed++; }
            }

            _logStore.Add("INFO",
                $"[PushQr] ✔ {pushed} pushed, {failed} failed → {lineIp} (line={lineId})",
                "RLink");
        }
        catch (Exception ex)
        {
            _logStore.Add("ERROR",
                $"[PushQr] Lỗi push QR → {lineIp}: {ex.Message}",
                "RLink");
        }
    }

    // ── Helper ───────────────────────────────────────────────────────
    private static string F(bool v) => v ? "✓" : "✗";
}