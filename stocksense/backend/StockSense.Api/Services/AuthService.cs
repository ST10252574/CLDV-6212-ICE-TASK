using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StockSense.Api.Data;
using StockSense.Api.Dtos;
using StockSense.Api.Models;

namespace StockSense.Api.Services;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Signing key. Must be at least 32 characters. Supplied through environment variables / secrets.</summary>
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "StockSense";
    public string Audience { get; set; } = "StockSenseUsers";
    public int ExpiryMinutes { get; set; } = 480;
}

public record AuthResult(bool Success, string? Error, AuthResponse? Response);

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
}

public class AuthService : IAuthService
{
    /// <summary>Custom claim that carries the numeric user id (avoids JWT claim-name remapping surprises).</summary>
    public const string UserIdClaim = "uid";

    private readonly AppDbContext _db;
    private readonly JwtSettings _jwt;

    public AuthService(AppDbContext db, IOptions<JwtSettings> jwt)
    {
        _db = db;
        _jwt = jwt.Value;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return new AuthResult(false, "An account with that email already exists.", null);
        }

        var user = new AppUser
        {
            Email = email,
            ShopName = request.ShopName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResult(true, null, BuildResponse(user));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such user" and "wrong password" so accounts cannot be enumerated.
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResult(false, "Invalid email or password.", null);
        }

        return new AuthResult(true, null, BuildResponse(user));
    }

    private AuthResponse BuildResponse(AppUser user) => new()
    {
        Token = CreateToken(user),
        Email = user.Email,
        ShopName = user.ShopName
    };

    private string CreateToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(UserIdClaim, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("shop", user.ShopName)
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
