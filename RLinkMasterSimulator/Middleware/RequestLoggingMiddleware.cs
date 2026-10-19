using RLinkMasterSimulator.Services;
using System.Text;

namespace RLinkMasterSimulator.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    // Các path không cần log body (quá nhiều hoặc không cần thiết)
    private static readonly HashSet<string> _skipBodyPaths =
    [
        "/api/sim/logs", "/api/sim/monitor/logs", "/api/sim/qrbank/status",
        "/api/health", "/swagger"
    ];

    public RequestLoggingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, SimLogStore logStore)
    {
        var start = DateTime.Now;
        var method = ctx.Request.Method;
        var path = ctx.Request.Path + ctx.Request.QueryString;
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "?";

        // ── Đọc request body nếu là POST/PUT ────────────────────────────
        string? reqBody = null;
        bool shouldReadBody = (method == "POST" || method == "PUT")
     && !_skipBodyPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (shouldReadBody && ctx.Request.ContentLength > 0)
        {
            ctx.Request.EnableBuffering();
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
            reqBody = await reader.ReadToEndAsync();
            ctx.Request.Body.Position = 0; // reset để controller đọc được
        }

        await _next(ctx);

        int status = ctx.Response.StatusCode;
        double ms = (DateTime.Now - start).TotalMilliseconds;
        string level = status >= 500 ? "ERROR" : status >= 400 ? "WARN" : "REQUEST";

        // ── Dòng 1: HTTP summary ─────────────────────────────────────────
        logStore.Add(level,
            $"{method} {path} → {status} ({ms:F0}ms) ← {ip}",
            "HTTP");

        // ── Dòng 2: Body JSON (nếu có) ──────────────────────────────────
        if (!string.IsNullOrWhiteSpace(reqBody))
        {
            var trimmed = reqBody.Length > 800
                ? reqBody[..800] + " …(truncated)"
                : reqBody;
            logStore.Add("INFO",
                $"  ↳ Body: {trimmed}",
                "HTTP");
        }
    }
}