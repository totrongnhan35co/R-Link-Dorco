using RLinkMasterSimulator.Models;

namespace RLinkMasterSimulator.Services;

/// <summary>Quản lý danh sách line giả lập (CRUD + active tracking).</summary>
public class SimLineStore
{
    private readonly SimFactoryStore _factoryStore;

    public SimLineStore(SimFactoryStore factoryStore)
    {
        _factoryStore = factoryStore;
    }

    private readonly List<LineInfo> _lines = new()
    {
        new() { factory_code = "1210", factory_name = "Nghệ An", line_id = "L01", line_name = "Line 01", line_ip = "", is_active = false },
         new() { factory_code = "1210", factory_name = "Nghệ An", line_id = "L02", line_name = "Line 02", line_ip = "", is_active = false },
          new() { factory_code = "1210", factory_name = "Nghệ An", line_id = "L03", line_name = "Line 03", line_ip = "", is_active = false },

        new() { factory_code = "1211", factory_name = "Lâm Đồng", line_id = "L01", line_name = "Line 01", line_ip = "", is_active = false },
          new() { factory_code = "1211", factory_name = "Lâm Đồng", line_id = "L02", line_name = "Line 02", line_ip = "", is_active = false },
            new() { factory_code = "1211", factory_name = "Lâm Đồng", line_id = "L03", line_name = "Line 03", line_ip = "", is_active = false },
              new() { factory_code = "1211", factory_name = "Lâm Đồng", line_id = "L04", line_name = "Line 04", line_ip = "", is_active = false },

    };
    private readonly object _lock = new();

    // ── Read ─────────────────────────────────────────────────────────
    public List<LineInfo> GetAll()
    {
        lock (_lock)
        {
            // Đồng bộ factory_name từ store mỗi lần đọc
            foreach (var l in _lines)
            {
                var f = _factoryStore.GetByCode(l.factory_code);
                if (f != null) l.factory_name = f.factory_name;
            }
            return new List<LineInfo>(_lines);
        }
    }

    public List<LineInfo> GetByFactory(string factoryCode)
    {
        lock (_lock) return _lines.Where(l => l.factory_code == factoryCode).ToList();
    }

    public LineInfo? GetById(string lineId, string factoryCode = null)
    {
        lock (_lock) return _lines.FirstOrDefault(l =>
            l.line_id == lineId &&
            (factoryCode == null || l.factory_code == factoryCode));
    }

    public LineInfo? GetActive() { lock (_lock) return _lines.FirstOrDefault(l => l.is_active); }

    // ── Add ──────────────────────────────────────────────────────────
    public bool Add(LineInfo line)
    {
        if (string.IsNullOrWhiteSpace(line.line_id)) return false;
        lock (_lock)
        {
            if (_lines.Any(l => l.line_id == line.line_id && l.factory_code == line.factory_code)) return false;
            // Tự điền factory_name nếu thiếu
            if (string.IsNullOrEmpty(line.factory_name))
            {
                var f = _factoryStore.GetByCode(line.factory_code);
                if (f != null) line.factory_name = f.factory_name;
            }
            _lines.Add(line);
            Console.WriteLine($"[SimLine] Added: [{line.factory_code}] {line.line_id} — {line.line_name}");
            return true;
        }
    }

    // ── Update ───────────────────────────────────────────────────────
    public bool Update(string lineId, string lineName, string lineIp, string factoryCode = "")
    {
        lock (_lock)
        {
            var line = _lines.FirstOrDefault(l => l.line_id == lineId
                && (string.IsNullOrEmpty(factoryCode) || l.factory_code == factoryCode));
            if (line == null) return false;
            line.line_name = lineName;
            line.line_ip = lineIp;
            if (!string.IsNullOrWhiteSpace(factoryCode))
            {
                line.factory_code = factoryCode;
                var f = _factoryStore.GetByCode(factoryCode);
                if (f != null) line.factory_name = f.factory_name;
            }
            return true;
        }
    }

    // ── Delete ───────────────────────────────────────────────────────
    public bool Delete(string lineId)
    {
        lock (_lock)
        {
            var line = _lines.FirstOrDefault(l => l.line_id == lineId);
            if (line == null) return false;
            _lines.Remove(line);
            return true;
        }
    }

    // ── SetActive: mỗi IP chỉ active 1 line duy nhất ─────────────────
    public (bool ok, string reason) SetActive(string lineId, string factoryCode, string lineIp)
    {
        lock (_lock)
        {
            var target = _lines.FirstOrDefault(l =>
                l.line_id == lineId &&
                (string.IsNullOrEmpty(factoryCode) || l.factory_code == factoryCode));

            if (target == null)
                return (false, $"Không tìm thấy line '{lineId}' (factory={factoryCode}).");

            if (!string.IsNullOrWhiteSpace(lineIp))
            {
                var conflict = _lines.FirstOrDefault(l =>
                    l.line_ip == lineIp &&
                    l.is_active &&
                    !(l.line_id == lineId && l.factory_code == target.factory_code));

                if (conflict != null)
                    return (false, $"IP {lineIp} đã active line '{conflict.line_id}' [{conflict.factory_code}].");
            }

            foreach (var l in _lines)
                l.is_active = false;

            target.is_active = true;
            target.line_ip = lineIp ?? string.Empty;

            Console.WriteLine($"[SimLine] Active: [{target.factory_code}] {target.line_id} ip={lineIp}");
            return (true, "ok");
        }
    }

    // ── Deactivate: hủy gán line ──────────────────────────────────────
    public (bool ok, string reason) Deactivate(string lineId, string factoryCode)
    {
        lock (_lock)
        {
            var target = _lines.FirstOrDefault(l =>
                l.line_id == lineId &&
                (string.IsNullOrEmpty(factoryCode) || l.factory_code == factoryCode));

            if (target == null)
                return (false, $"Không tìm thấy line '{lineId}'.");

            target.is_active = false;
            target.line_ip = string.Empty;

            Console.WriteLine($"[SimLine] Deactivated: [{target.factory_code}] {target.line_id}");
            return (true, "ok");
        }
    }
}
