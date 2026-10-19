using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLinkMasterSimulator.Models;
using RLinkMasterSimulator.Services;
using System.Text.Json;

namespace RLinkMasterSimulator.Controllers;

[ApiController]
[Route("api/sim")]
[AllowAnonymous]
public class SimController : ControllerBase
{
    private readonly SimQrStore _qrStore;
    private readonly SimRLinkConfig _config;
    private readonly SimMonitorLog _monitorLog;
    private readonly SimLineStore _lineStore;
    private readonly SimFactoryStore _factoryStore;
    private readonly IHttpClientFactory _http;
    private readonly AccountStore _accountStore;

    public SimController(SimQrStore qrStore, SimRLinkConfig config,
        SimMonitorLog monitorLog, SimLineStore lineStore, SimFactoryStore factoryStore,
        IHttpClientFactory http, AccountStore accountStore)
    {
        _qrStore = qrStore;
        _config = config;
        _monitorLog = monitorLog;
        _lineStore = lineStore;
        _factoryStore = factoryStore;
        _http = http;
        _accountStore = accountStore;
    }

    // ── QR Bank ─────────────────────────────────────────────────────

    [HttpGet("qrbank/status")]
    public IActionResult QrBankStatus() => Ok(new
    {
        available = _qrStore.Available,
        totalGenerated = _qrStore.TotalGenerated,
        totalAllocated = _qrStore.TotalAllocated,
    });

    [HttpPost("qrbank/generate")]
    public IActionResult Generate([FromQuery] int count = 100, [FromQuery] string prefix = "THSIM")
    {
        if (count < 1 || count > 100_000)
            return BadRequest(new { error = "count phải trong khoảng 1–100000" });
        _qrStore.Generate(count, prefix);
        return Ok(new { generated = count, available = _qrStore.Available });
    }

    [HttpDelete("qrbank/clear")]
    public IActionResult Clear()
    {
        _qrStore.Clear();
        return Ok(new { message = "Đã xóa kho QR.", available = 0 });
    }




    /// <summary>
    /// POST /api/rlink/mark-used
    /// R-Link client gọi khi tạo job / đổi QR để đánh dấu QR đã dùng.
    /// Yêu cầu Bearer token (login qua /api/auth/login trước).
    /// </summary>
    [HttpPost("mark-used")]
    public IActionResult MarkQrUsed([FromBody] MarkQrUsedRequest req)
    {
        if (req?.QrCodes == null || req.QrCodes.Count == 0)
            return BadRequest(new { success = false, error = "Cần ít nhất 1 QR code." });

        int marked = 0;
        foreach (var qr in req.QrCodes)
        {
            if (string.IsNullOrWhiteSpace(qr)) continue;
            _qrStore.MarkUsed(qr, req.JobName, req.LineId, req.LineName,
                req.FactoryCode, req.Batch, req.RLinkName);
            marked++;
        }

        Console.WriteLine($"[RLink][MarkUsed] {marked} QR | job='{req.JobName}'" +
                          $" line='{req.LineId}' batch='{req.Batch}'");

        return Ok(new
        {
            success = true,
            marked,
            total_used = _qrStore.TotalUsed,
        });
    }

    /// <summary>
    /// POST /api/rlink/mark-unused
    /// R-Link client gọi khi CompleteJob với 0 sản phẩm để giải phóng QR.
    /// </summary>
    [HttpPost("mark-unused")]
    public IActionResult MarkQrUnused([FromBody] MarkQrUnusedRequest req)
    {
        if (req?.QrCodes == null || req.QrCodes.Count == 0)
            return BadRequest(new { success = false, error = "Cần ít nhất 1 QR code." });

        int released = _qrStore.MarkUnused(req.QrCodes, req.JobName ?? "");

        Console.WriteLine($"[RLink][MarkUnused] {released} QR released | job='{req.JobName}'");

        return Ok(new
        {
            success = true,
            released,
            total_used = _qrStore.TotalUsed,
        });
    }

    /// <summary>GET /api/sim/qrbank/used-log — lấy danh sách QR đã mark used.</summary>
    [HttpGet("qrbank/used-log")]
    public IActionResult GetUsedLog([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        take = Math.Min(take, 500);
        var log = _qrStore.GetUsedLog(skip, take);
        return Ok(new { total = _qrStore.TotalUsed, data = log });
    }








    // ── Push QR Batch — tự login trước rồi push ─────────────────────────
    [HttpPost("push/batch")]
    public async Task<IActionResult> PushQrBatch([FromBody] PushQrRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RLinkIp))
            return BadRequest(new { error = "rlinkIp là bắt buộc." });

        var qrCodes = _qrStore.Allocate(req.Count);
        if (qrCodes.Count == 0)
            return BadRequest(new { error = "Kho QR trống." });

        string baseUrl = $"http://{req.RLinkIp}:{req.RLinkPort}";
        var client = _http.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        // ── Bước 1: Lấy Bearer token (login) ────────────────────────
        string bearerToken = req.BearerToken;
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            try
            {
                var loginBody = JsonSerializer.Serialize(new
                {
                    username = "qrbank",
                    password = "qrbank@#062026",
                    secret_key = "NUzfz%sqL%j&0sTMfi2oYPWnTlV8wYW6TJcIssOclIVTQpG"
                });
                var loginResp = await client.PostAsync(
                    $"{baseUrl}/api/qrbank/login",
                    new StringContent(loginBody, System.Text.Encoding.UTF8, "application/json"));

                if (loginResp.IsSuccessStatusCode)
                {
                    string loginJson = await loginResp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(loginJson);
                    bearerToken = doc.RootElement.TryGetProperty("access_token", out var t)
                        ? t.GetString() : "";
                }
                else
                {
                    string errBody = await loginResp.Content.ReadAsStringAsync();
                    _qrStore.Return(qrCodes);
                    return StatusCode((int)loginResp.StatusCode,
                        new { error = $"Login thất bại ({(int)loginResp.StatusCode}): {errBody}" });
                }
            }
            catch (Exception ex)
            {
                _qrStore.Return(qrCodes);
                return StatusCode(503, new { error = $"Login thất bại: {ex.Message}" });
            }
        }

        if (!string.IsNullOrWhiteSpace(bearerToken))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

        // ── Bước 2: Push từng mã đến /api/qrbank/data ───────────────
        int ok = 0, duplicates = 0, fail = 0;
        var failed = new List<string>();

        foreach (var qr in qrCodes)
        {
            var body = JsonSerializer.Serialize(new
            {
                qr_code = qr,
                factory_code = req.FactoryCode,
                line_id = req.LineId,
                line_name = req.LineName,
                batch = req.Batch ?? string.Empty
            });
            try
            {
                var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                var resp = await client.PostAsync($"{baseUrl}/api/qrbank/data", content);
                string respJson = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    bool isDup = false;
                    try
                    {
                        using var doc = JsonDocument.Parse(respJson);
                        isDup = doc.RootElement.TryGetProperty("duplicate", out var d) && d.GetBoolean();
                    }
                    catch { }

                    if (isDup) duplicates++;
                    else ok++;
                }
                else { fail++; failed.Add(qr); }
            }
            catch { fail++; failed.Add(qr); }
        }

        if (failed.Count > 0)
            _qrStore.Return(failed);

        Console.WriteLine($"[Sim] PushBatch → {req.RLinkIp} ok={ok} dup={duplicates} fail={fail} remaining={_qrStore.Available}");
        return Ok(new
        {
            ok,
            duplicates,
            fail,
            remaining = _qrStore.Available
        });
    }

    // ── Settings ─────────────────────────────────────────────────────

    [HttpGet("settings")]
    public IActionResult GetSettings() => Ok(_config.Settings);

    [HttpPut("settings")]
    public IActionResult UpdateSettings([FromBody] RLinkSettings settings)
    {
        _config.Settings = settings;
        return Ok(new { success = true });
    }

    // ── QUẢN LÝ ACCOUNTS ─────────────────────────────────────────────

    [HttpGet("accounts")]
    public IActionResult GetAccounts() => Ok(_accountStore.AllAccountInfos());

    [HttpPost("accounts")]
    public IActionResult AddAccount([FromBody] SimAccount account)
    {
        if (string.IsNullOrWhiteSpace(account.Username) || string.IsNullOrWhiteSpace(account.Password))
            return BadRequest(new { error = "Username và Password là bắt buộc." });

        bool ok = _accountStore.Add(account);
        if (!ok) return Conflict(new { error = $"Username '{account.Username}' đã tồn tại." });
        return Ok(new { success = true });
    }

    [HttpPut("accounts/{username}")]
    public IActionResult UpdateAccount(string username, [FromBody] SimAccount account)
    {
        bool ok = _accountStore.Update(username, account.FullName, account.Role, account.Password);
        if (!ok) return NotFound(new { error = $"Không tìm thấy tài khoản: {username}" });
        return Ok(new { success = true });
    }

    [HttpDelete("accounts/{username}")]
    public IActionResult DeleteAccount(string username)
    {
        bool ok = _accountStore.Delete(username);
        if (!ok) return NotFound(new { error = $"Không tìm thấy tài khoản: {username}" });
        return Ok(new { success = true });
    }

    // ── Monitor Log ──────────────────────────────────────────────────

    [HttpGet("monitor/logs")]
    public IActionResult GetMonitorLogs([FromQuery] int count = 50)
        => Ok(_monitorLog.GetRecent(count));

    [HttpDelete("monitor/logs")]
    public IActionResult ClearMonitorLogs() { _monitorLog.Clear(); return Ok(); }

    // ── QUẢN LÝ FACTORIES ────────────────────────────────────────────

    [HttpGet("factories")]
    public IActionResult GetFactories() => Ok(_factoryStore.GetAll());

    [HttpPost("factories")]
    public IActionResult AddFactory([FromBody] SimFactory factory)
    {
        if (string.IsNullOrWhiteSpace(factory.factory_code))
            return BadRequest(new { error = "factory_code không được để trống." });

        bool ok = _factoryStore.Add(factory);
        if (!ok) return Conflict(new { error = $"factory_code '{factory.factory_code}' đã tồn tại." });
        return Ok(new { success = true, factory });
    }

    [HttpPut("factories/{code}")]
    public IActionResult UpdateFactory(string code, [FromBody] SimFactory factory)
    {
        bool ok = _factoryStore.Update(code, factory.factory_name);
        if (!ok) return NotFound(new { error = $"Không tìm thấy nhà máy: {code}" });
        return Ok(new { success = true });
    }

    [HttpDelete("factories/{code}")]
    public IActionResult DeleteFactory(string code)
    {
        bool ok = _factoryStore.Delete(code);
        if (!ok) return NotFound(new { error = $"Không tìm thấy nhà máy: {code}" });
        return Ok(new { success = true });
    }

    // ── QUẢN LÝ LINES ────────────────────────────────────────────────

    [HttpGet("lines")]
    public IActionResult GetLines([FromQuery] string? factory_code = null)
    {
        var lines = string.IsNullOrWhiteSpace(factory_code)
            ? _lineStore.GetAll()
            : _lineStore.GetByFactory(factory_code);
        return Ok(lines);
    }

    [HttpPost("lines")]
    public IActionResult AddLine([FromBody] LineInfo line)
    {
        if (string.IsNullOrWhiteSpace(line.line_id))
            return BadRequest(new { error = "line_id không được để trống." });

        bool ok = _lineStore.Add(line);
        if (!ok) return Conflict(new { error = $"line_id '{line.line_id}' đã tồn tại." });
        return Ok(new { success = true, line });
    }

    [HttpPut("lines/{lineId}")]
    public IActionResult UpdateLine(string lineId, [FromBody] LineInfo line)
    {
        bool ok = _lineStore.Update(lineId, line.line_name, line.line_ip, line.factory_code);
        if (!ok) return NotFound(new { error = $"Không tìm thấy line: {lineId}" });
        return Ok(new { success = true });
    }

    [HttpDelete("lines/{lineId}")]
    public IActionResult DeleteLine(string lineId)
    {
        bool ok = _lineStore.Delete(lineId);
        if (!ok) return NotFound(new { error = $"Không tìm thấy line: {lineId}" });
        return Ok(new { success = true });
    }

    [HttpPost("lines/{lineId}/assign")]
    public IActionResult AssignLine(string lineId, [FromQuery] string? factory_code = null, [FromQuery] string? line_ip = null)
    {
        var (ok, reason) = _lineStore.SetActive(lineId, factory_code ?? "", line_ip ?? "");
        if (!ok) return NotFound(new { error = reason });
        return Ok(new { success = true, active_line = lineId });
    }
}