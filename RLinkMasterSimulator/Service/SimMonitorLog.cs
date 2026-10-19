using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RLinkMasterSimulator.Models;

namespace RLinkMasterSimulator.Services;

/// <summary>Lưu trữ các bản ghi monitor nhận được gần nhất (tối đa 200).</summary>
public class SimMonitorLog
{
    private readonly LinkedList<MonitorEntry> _entries = new();
    private readonly object _lock = new();
    private const int MaxEntries = 200;

    public void Add(MonitorPayload payload)
    {
        lock (_lock)
        {
            _entries.AddFirst(new MonitorEntry
            {
                ReceivedAt = DateTime.Now,
                RLinkName = payload.RLinkName,
                LineId = payload.LineId,
                CurrentBatch = payload.CurrentBatch ?? "-",
                TotalProduced = payload.TotalProduced,
                TotalQrUsed = payload.TotalQrUsed,
                TotalQrFailed = payload.TotalQrFailed,
                IsPrinterConnected = payload.IsPrinterConnected,
                IsCameraConnected = payload.IsCameraConnected,
                IsPlcConnected = payload.IsPlcConnected,
            });
            if (_entries.Count > MaxEntries)
                _entries.RemoveLast();
        }
    }

    public List<MonitorEntry> GetRecent(int count = 50)
    {
        lock (_lock)
            return _entries.Take(count).ToList();
    }

    public void Clear()
    {
        lock (_lock) { _entries.Clear(); }
    }
}

public class MonitorEntry
{
    public DateTime ReceivedAt { get; set; }
    public string RLinkName { get; set; } = "";
    public string LineId { get; set; } = "";
    public string CurrentBatch { get; set; } = "";
    public int TotalProduced { get; set; }
    public int TotalQrUsed { get; set; }
    public int TotalQrFailed { get; set; }
    public bool IsPrinterConnected { get; set; }
    public bool IsCameraConnected { get; set; }
    public bool IsPlcConnected { get; set; }
}