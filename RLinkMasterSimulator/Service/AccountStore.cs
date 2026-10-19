using RLinkMasterSimulator.Models;

namespace RLinkMasterSimulator.Services;

/// <summary>Quản lý tài khoản giả lập — singleton, hỗ trợ CRUD từ Dashboard.</summary>
public class AccountStore
{
    private readonly object _lock = new();

    private readonly List<SimAccount> _accounts = new()
    {
        new() { Username = "config",        Password = "config@123",   Role = "Config",   FullName = "Tài khoản Config" },
        new() { Username = "admin",         Password = "123456",       Role = "Admin",     FullName = "Quản trị viên"    },
        new() { Username = "Administrator", Password = "Admin@2025",   Role = "Admin",     FullName = "Administrator"    },
        new() { Username = "Support",       Password = "Support@2025", Role = "Config",   FullName = "Support"          },
        new() { Username = "operator",      Password = "operator@123", Role = "Operator",  FullName = "Vận hành Line 1"  },
        new() { Username = "Operator",      Password = "Operator@123", Role = "Operator",  FullName = "Operator"         },

    };

    // ── Validate ─────────────────────────────────────────────────────
    public bool Validate(string username, string password, out string role, out string fullName)
    {
        lock (_lock)
        {
            role = fullName = "";
            var acc = _accounts.FirstOrDefault(a =>
                string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase)
                && a.Password == password);
            if (acc == null) return false;
            role = acc.Role;
            fullName = acc.FullName;
            return true;
        }
    }

    // ── CRUD ──────────────────────────────────────────────────────────
    public List<SimAccount> GetAll() { lock (_lock) return new List<SimAccount>(_accounts); }

    public bool Add(SimAccount account)
    {
        if (string.IsNullOrWhiteSpace(account.Username)) return false;
        lock (_lock)
        {
            if (_accounts.Any(a => string.Equals(a.Username, account.Username, StringComparison.OrdinalIgnoreCase)))
                return false;
            _accounts.Add(account);
            return true;
        }
    }

    public bool Update(string username, string fullName, string role, string? newPassword)
    {
        lock (_lock)
        {
            var acc = _accounts.FirstOrDefault(a =>
                string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));
            if (acc == null) return false;
            acc.FullName = fullName;
            acc.Role = role;
            if (!string.IsNullOrWhiteSpace(newPassword))
                acc.Password = newPassword;
            return true;
        }
    }

    public bool Delete(string username)
    {
        lock (_lock)
        {
            var acc = _accounts.FirstOrDefault(a =>
                string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));
            if (acc == null) return false;
            _accounts.Remove(acc);
            return true;
        }
    }

    // ── Permissions ───────────────────────────────────────────────────
    public static Dictionary<string, bool> PermissionsFor(string role) => role switch
    {
        "Config" => new()
        {
            ["settings"] = true,
            ["controls"] = false,
            ["exports"] = false,
            ["accounts"] = false,
            ["createJob"] = false,
            ["deleteJob"] = false,
            ["productionSettings"] = true
        },
        "Admin" => new()
        {
            ["settings"] = true,
            ["controls"] = true,
            ["exports"] = true,
            ["accounts"] = true,
            ["createJob"] = true,
            ["deleteJob"] = true,
            ["productionSettings"] = false
        },
        _ => new()
        {
            ["settings"] = false,
            ["controls"] = true,
            ["exports"] = false,
            ["accounts"] = false,
            ["createJob"] = true,
            ["deleteJob"] = false,
            ["productionSettings"] = false
        },
    };

    // ── Dashboard display ─────────────────────────────────────────────
    public IEnumerable<object> AllAccountInfos() =>
        GetAll().Select(a => new
        {
            username = a.Username,
            full_name = a.FullName,
            role = a.Role,
            permissions = PermissionsFor(a.Role)
        });
}