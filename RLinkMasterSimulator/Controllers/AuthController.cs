using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RLinkMasterSimulator.Models;
using RLinkMasterSimulator.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RLinkMasterSimulator.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly AccountStore _accounts;

    public AuthController(IConfiguration config, AccountStore accounts)
    {
        _config = config;
        _accounts = accounts;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new LoginResult { IsSuccess = false, Message = "username và password là bắt buộc." });

        if (!_accounts.Validate(request.Username, request.Password, out string role, out string fullName))
            return Ok(new LoginResult { IsSuccess = false, Message = "Sai tên đăng nhập hoặc mật khẩu." });

        int accessExpiry = int.TryParse(_config["Jwt:AccessTokenExpirySeconds"], out int a) ? a : 86400;
        int refreshExpiry = int.TryParse(_config["Jwt:RefreshTokenExpirySeconds"], out int r) ? r : 2592000;

        return Ok(new LoginResult
        {
            IsSuccess = true,
            Username = request.Username,
            FullName = fullName,
            Role = role,
            AccessToken = GenerateToken(request.Username, role, accessExpiry: accessExpiry),
            RefreshToken = GenerateToken(request.Username, role, accessExpiry: refreshExpiry, type: "refresh"),
            Permissions = AccountStore.PermissionsFor(role),
        });
    }

    [HttpPost("refresh-token")]
    public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.refresh_token))
            return BadRequest(new LoginResult { IsSuccess = false, Message = "refresh_token là bắt buộc." });

        if (!ValidateToken(request.refresh_token, out string? username, out string? role, out string? err))
            return Unauthorized(new LoginResult { IsSuccess = false, Message = err ?? "Token không hợp lệ." });

        int accessExpiry = int.TryParse(_config["Jwt:AccessTokenExpirySeconds"], out int a) ? a : 86400;
        int refreshExpiry = int.TryParse(_config["Jwt:RefreshTokenExpirySeconds"], out int r) ? r : 2592000;

        return Ok(new LoginResult
        {
            IsSuccess = true,
            Username = username,
            AccessToken = GenerateToken(username!, role!, accessExpiry: accessExpiry),
            RefreshToken = GenerateToken(username!, role!, accessExpiry: refreshExpiry, type: "refresh"),
        });
    }

    [HttpGet("accounts")]
    [Authorize]
    public IActionResult GetAccounts()
    {
        var accounts = _accounts.AllAccountInfos().ToList();
        return Ok(new
        {
            success = true,
            message = $"Lấy danh sách tài khoản thành công. Tổng: {accounts.Count}.",
            total = accounts.Count,
            data = accounts
        });
    }

    private string GenerateToken(string username, string role,
        int accessExpiry = 86400, string type = "access")
    {
        string secret = _config["Jwt:Secret"] ?? "RLinkMasterSimulator_JWTSecret_2026!!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: new[]
            {
                new Claim("username", username),
                new Claim(ClaimTypes.Role, role),
                new Claim("type", type),
            },
            expires: DateTime.UtcNow.AddSeconds(accessExpiry),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private bool ValidateToken(string token, out string? username, out string? role, out string? error)
    {
        username = null; role = null; error = null;
        try
        {
            string secret = _config["Jwt:Secret"] ?? "RLinkMasterSimulator_JWTSecret_2026!!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);
            username = principal.FindFirst("username")?.Value;
            role = principal.FindFirst(ClaimTypes.Role)?.Value;
            return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }
}