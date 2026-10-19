using RLinkMasterSimulator.Models;

namespace RLinkMasterSimulator.Services;

/// <summary>Quản lý danh sách nhà máy giả lập (CRUD in-memory).</summary>
public class SimFactoryStore
{
    private readonly List<SimFactory> _factories = new()
    {
        new() { factory_code = "1210", factory_name = "Nghệ An" },
         new() { factory_code = "1211", factory_name = "Lâm Đồng" },
        new() { factory_code = "1240", factory_name = "Hưng Yên"  },
        //new() { factory_code = "1260", factory_name = "Gia Lai"   },
        //new() { factory_code = "1212", factory_name = "Đà Nẵng"   },
    };
    private readonly object _lock = new();

    public List<SimFactory> GetAll()
    {
        lock (_lock) return new List<SimFactory>(_factories);
    }

    public SimFactory? GetByCode(string code)
    {
        lock (_lock) return _factories.FirstOrDefault(f => f.factory_code == code);
    }

    /// <summary>Thêm nhà máy. Trả false nếu factory_code đã tồn tại.</summary>
    public bool Add(SimFactory factory)
    {
        if (string.IsNullOrWhiteSpace(factory.factory_code)) return false;
        lock (_lock)
        {
            if (_factories.Any(f => f.factory_code == factory.factory_code)) return false;
            _factories.Add(factory);
            Console.WriteLine($"[SimFactory] Added: {factory.factory_code} — {factory.factory_name}");
            return true;
        }
    }

    /// <summary>Cập nhật tên nhà máy. Trả false nếu không tìm thấy.</summary>
    public bool Update(string code, string newName)
    {
        lock (_lock)
        {
            var f = _factories.FirstOrDefault(x => x.factory_code == code);
            if (f == null) return false;
            f.factory_name = newName;
            Console.WriteLine($"[SimFactory] Updated: {code} → {newName}");
            return true;
        }
    }

    /// <summary>Xóa nhà máy. Trả false nếu không tìm thấy.</summary>
    public bool Delete(string code)
    {
        lock (_lock)
        {
            var f = _factories.FirstOrDefault(x => x.factory_code == code);
            if (f == null) return false;
            _factories.Remove(f);
            Console.WriteLine($"[SimFactory] Deleted: {code}");
            return true;
        }
    }
}