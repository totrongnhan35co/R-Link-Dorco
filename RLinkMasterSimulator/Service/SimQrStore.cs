using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RLinkMasterSimulator.Services;

/// <summary>Kho QR in-memory, thread-safe, FIFO.</summary>
public class SimQrStore
{
    private readonly Queue<string> _queue = new();
    private readonly object _lock = new();
    private int _totalGenerated;
    private int _totalAllocated;
    private readonly List<QrUsedRecord> _usedLog = new();

    public int Available { get { lock (_lock) return _queue.Count; } }
    public int TotalGenerated => _totalGenerated;
    public int TotalAllocated => _totalAllocated;
    public int TotalUsed { get { lock (_lock) return _usedLog.Count; } }

    public void Generate(int count, string prefix = "THSIM")
    {
        lock (_lock)
        {
            for (int i = 0; i < count; i++)
            {
                int seq = Interlocked.Increment(ref _totalGenerated);
                _queue.Enqueue($"{prefix}-{DateTime.Now:yyyyMMdd}-{seq:D6}");
            }
        }
    }

    public List<string> Allocate(int count)
    {
        var result = new List<string>();
        lock (_lock)
        {
            int take = Math.Min(count, _queue.Count);
            for (int i = 0; i < take; i++)
            {
                result.Add(_queue.Dequeue());
                _totalAllocated++;
            }
        }
        return result;
    }

    /// <summary>Trả lại các QR code vào đầu hàng đợi khi push thất bại.</summary>
    public void Return(IEnumerable<string> codes)
    {
        lock (_lock)
        {
            var existing = _queue.ToList();
            _queue.Clear();
            foreach (var c in codes)
            {
                _queue.Enqueue(c);
                _totalAllocated--;
            }
            foreach (var c in existing)
                _queue.Enqueue(c);
        }
    }

    public void Import(IEnumerable<string> codes)
    {
        lock (_lock)
        {
            foreach (var c in codes)
            {
                _queue.Enqueue(c);
                Interlocked.Increment(ref _totalGenerated);
            }
        }
    }

    public void Clear()
    {
        lock (_lock) { _queue.Clear(); }
    }

    // ── Mark is_used ─────────────────────────────────────────────────

    /// <summary>
    /// Đánh dấu QR code đã được sử dụng khi R-Link tạo job.
    /// Lưu vào _usedLog in-memory để Dashboard và API có thể truy vấn.
    /// </summary>
    public void MarkUsed(string qrCode, string jobName, string lineId,
        string lineName, string factoryCode, string batch, string rlinkName = "")
    {
        if (string.IsNullOrWhiteSpace(qrCode)) return;
        lock (_lock)
        {
            _usedLog.Add(new QrUsedRecord
            {
                QrCode = qrCode,
                JobName = jobName,
                LineId = lineId,
                LineName = lineName,
                FactoryCode = factoryCode,
                Batch = batch,
                RLinkName = rlinkName,
                UsedAt = DateTime.Now
            });
        }
    }

    /// <summary>Lấy danh sách QR đã dùng, mới nhất trước.</summary>
    public List<QrUsedRecord> GetUsedLog(int skip = 0, int take = 100)
    {
        lock (_lock)
            return _usedLog
                .OrderByDescending(r => r.UsedAt)
                .Skip(skip).Take(take)
                .ToList();
    }

    // ── MarkUnused ────────────────────────────────────────────────────

    /// <summary>Gỡ đánh dấu QR khi job complete với 0 sản phẩm.</summary>
    public int MarkUnused(List<string> qrCodes, string jobName)
    {
        if (qrCodes == null || qrCodes.Count == 0) return 0;
        int count = 0;
        lock (_lock)
        {
            foreach (var qr in qrCodes)
            {
                int removed = _usedLog.RemoveAll(r => r.QrCode == qr && r.JobName == jobName);
                count += removed;
            }
        }
        return count;
    }
}

public class QrUsedRecord
{
    public string QrCode { get; set; } = "";
    public string JobName { get; set; } = "";
    public string LineId { get; set; } = "";
    public string LineName { get; set; } = "";
    public string FactoryCode { get; set; } = "";
    public string Batch { get; set; } = "";
    public string RLinkName { get; set; } = "";
    public DateTime UsedAt { get; set; }
}