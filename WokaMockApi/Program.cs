using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Cấu hình test mode qua query string hoặc header
// ?mode=success        -> luôn trả 200
// ?mode=apierror       -> trả lỗi API (code != 200) ngẫu nhiên
// ?mode=networkerror   -> drop connection (không trả response)
// ?mode=randomerror    -> xen kẽ success/error
// Header X-Test-Mode cũng được hỗ trợ

int requestCounter = 0;
object counterLock = new object();

string GetMode(HttpRequest request)
{
    string mode = "success";
    if (request.Headers.TryGetValue("X-Test-Mode", out var headerMode) && !string.IsNullOrEmpty(headerMode))
    {
        mode = headerMode.ToString().ToLowerInvariant();
    }
    else if (request.Query.TryGetValue("mode", out var queryMode) && !string.IsNullOrEmpty(queryMode))
    {
        mode = queryMode.ToString().ToLowerInvariant();
    }
    return mode;
}

bool ShouldFail(string mode)
{
    lock (counterLock)
    {
        requestCounter++;
        return mode switch
        {
            "apierror" => true,
            "networkerror" => true,
            "randomerror" => requestCounter % 3 == 0, // cứ 3 request lại fail 1
            _ => false
        };
    }
}

app.MapGet("/", () => "Woka Mock API is running. Test modes: success, apierror, networkerror, randomerror");

app.MapPost("/cargos", async (HttpContext context) =>
{
    var mode = GetMode(context.Request);

    if (mode == "networkerror")
    {
        // Giả lập mất kết nối: abort request
        context.Abort();
        return;
    }

    if (ShouldFail(mode))
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsJsonAsync(new
        {
            code = 500,
            message = "Mock API error",
            payload = (object?)null
        });
        return;
    }

    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    // Log request
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] POST /cargos | mode={mode} | bodyLength={body.Length}");
    try
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        if (payload.TryGetProperty("cargoCode", out var cargoCode) && payload.TryGetProperty("qrCodeList", out var qrList))
        {
            Console.WriteLine($"  cargoCode={cargoCode.GetString()} | qrCount={qrList.GetArrayLength()}");
        }
    }
    catch { }

    context.Response.StatusCode = 200;
    await context.Response.WriteAsJsonAsync(new
    {
        code = 200,
        message = "OK",
        payload = new
        {
            id = Guid.NewGuid().ToString("N"),
            cargoCode = $"CARGO-{requestCounter}",
            status = 1,
            createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        }
    });
});

app.MapPost("/packaging-sessions/cancel-list", async (HttpContext context) =>
{
    var mode = GetMode(context.Request);

    if (mode == "networkerror")
    {
        context.Abort();
        return;
    }

    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] POST /packaging-sessions/cancel-list | mode={mode} | bodyLength={body.Length}");
    try
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        if (payload.TryGetProperty("listCancelQr", out var list))
        {
            Console.WriteLine($"  cancelCount={list.GetArrayLength()}");
        }
    }
    catch { }

    context.Response.StatusCode = 200;
    await context.Response.WriteAsJsonAsync(new
    {
        code = 200,
        message = "Cancel list accepted",
        payload = ""
    });
});

app.MapGet("/health", () => new { status = "ok", counter = requestCounter });

var port = builder.Configuration["Urls"] ?? "http://localhost:5555";
Console.WriteLine($"Woka Mock API starting on {port}");
Console.WriteLine("Test modes: success, apierror, networkerror, randomerror");
Console.WriteLine("Use ?mode=<mode> query string or X-Test-Mode header");

app.Run(port);
