using Microsoft.AspNetCore.Mvc;
using RLinkMasterSimulator.Services;

namespace RLinkMasterSimulator.Controllers;

[ApiController]
[Route("api/sim/logs")]
public class LogController : ControllerBase
{
    private readonly SimLogStore _store;

    public LogController(SimLogStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Lấy danh sách server log mới nhất.
    /// Endpoint: <c>GET api/sim/logs</c> — param <c>count</c> (mặc định 100).
    /// </summary>
    [HttpGet]
    public IActionResult GetLogs([FromQuery] int count = 100)
        => Ok(_store.GetLatest(count));

    /// <summary>
    /// Xóa toàn bộ server log.
    /// Endpoint: <c>DELETE api/sim/logs</c>
    /// </summary>
    [HttpDelete]
    public IActionResult ClearLogs()
    {
        _store.Clear();
        return Ok(new { ok = true });
    }
}