using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLinkMasterSimulator.Models;
using RLinkMasterSimulator.Services;

namespace RLinkMasterSimulator.Controllers;

[ApiController]
[Route("api/job")]
[Authorize]
public class JobController : ControllerBase
{
    private readonly SimRLinkConfig _config;
    private readonly ILogger<JobController> _logger;

    // Ký tự hợp lệ cho phần serial trong QR GS1 (loại bỏ các ký tự dễ nhầm)
    private static readonly char[] _serialChars =
        "abcdefghjkmnpqrstuvwxyz0123456789".ToCharArray();

    public JobController(SimRLinkConfig config, ILogger<JobController> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/job/complete
    /// Nhận thống kê job hoàn thành, kiểm tra QrThreshold,
    /// cấp phát QR mới nếu cần.
    /// </summary>
    [HttpPost("complete")]
    public IActionResult CompleteJob([FromBody] CompleteJobRequest request)
    {
        if (request == null)
            return BadRequest(new CompleteJobResult { IsSuccess = false, Message = "Request không hợp lệ." });

        try
        {
            // Dùng QrRemainingInDb từ client (đã đếm PG), không tự tính lại
            int qrRemaining = request.QrRemainingInDb >= 0 ? request.QrRemainingInDb : 0;

           

            int threshold = _config.Settings.QrThreshold;
           
            bool shouldAllocate = qrRemaining < threshold;


            _logger.LogInformation(
                "[CompleteJob] Job={Job} | Used={Used} | Total={Total} | Remaining={Rem} | Threshold={Thr} | Allocate={Alloc}",
                request.JobName, request.QrUsedCount, request.QrTotalAllocated,
                qrRemaining, threshold, shouldAllocate);

            var result = new CompleteJobResult { IsSuccess = true };

            if (shouldAllocate)
            {
                // Cấp phát: (threshold - còn lại) + 10 để đảm bảo vượt ngưỡng
                int allocateCount = qrRemaining < threshold ? (threshold - qrRemaining) + 20 : 0;
                var newQrCodes = GenerateQrCodes(allocateCount, request.ProductId,
                                                  request.LineId, request.BatchNo);

                result.AllocatedQrCodes = newQrCodes;
                result.Message = $"Hoàn thành job thành công. Cấp {newQrCodes.Count} QR mới " +
                                 $"(còn {qrRemaining}, ngưỡng {threshold}).";

                _logger.LogInformation("[CompleteJob] ✔ Cấp {Count} QR mới cho job {Job}.",
                    newQrCodes.Count, request.JobName);
            }
            else
            {
                result.AllocatedQrCodes = new List<string>();
                result.Message = $"Hoàn thành job thành công. " +
                                 $"Không cần cấp QR mới (còn {qrRemaining} ≥ ngưỡng {threshold}).";

                _logger.LogInformation("[CompleteJob] ✔ Không cấp QR mới (còn đủ).");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CompleteJob] Lỗi xử lý.");
            return StatusCode(500, new CompleteJobResult { IsSuccess = false, Message = $"Lỗi server: {ex.Message}" });
        }
    }

    // ── Helper: sinh QR codes đúng định dạng GS1 URL TH True Milk ──────────
    // Format: https://ndatrace.vn/01/{gtin}/21/{serial10chars}
    private List<string> GenerateQrCodes(int count, string productId,
                                          string lineId, string batchNo)
    {
        var list = new List<string>(count);

        // Dùng ProductId làm GTIN; fallback về mã mặc định nếu rỗng
        string gtin = string.IsNullOrWhiteSpace(productId) ? "8935217402816" : productId;

        var rng = new Random();
        var generated = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < count; i++)
        {
            string serial;
            // Sinh serial ngẫu nhiên 10 ký tự, đảm bảo không trùng trong batch này
            do
            {
                serial = GenerateSerial(rng, 10);
            }
            while (!generated.Add(serial));

            list.Add($"https://ndatrace.vn/01/{gtin}/21/{serial}");
        }

        _logger.LogInformation(
            "[GenerateQrCodes] Sinh {Count} QR | GTIN={Gtin} | Line={Line} | Batch={Batch}",
            count, gtin, lineId, batchNo);

        return list;
    }

    private string GenerateSerial(Random rng, int length)
    {
        var chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = _serialChars[rng.Next(_serialChars.Length)];
        return new string(chars);
    }
}