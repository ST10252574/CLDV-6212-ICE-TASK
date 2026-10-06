using Microsoft.Extensions.Options;
using StockSense.Api.Dtos;
using StockSense.Api.Services;

namespace StockSense.Tests;

public class AuthServiceTests
{
    private static AuthService NewService(StockSense.Api.Data.AppDbContext db) =>
        new(db, Options.Create(new JwtSettings
        {
            Key = "unit-test-signing-key-that-is-at-least-32-chars",
            Issuer = "test",
            Audience = "test"
        }));

    [Fact]
    public async Task Register_CreatesAccount_AndReturnsToken()
    {
        using var db = TestHelpers.NewDb();
        var auth = NewService(db);

        var result = await auth.RegisterAsync(new RegisterRequest
        {
            Email = "Owner@Shop.co.za",
            Password = "Password123!",
            ShopName = "Corner Shop"
        });

        Assert.True(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Response!.Token));
        Assert.Equal("owner@shop.co.za", result.Response.Email); // normalised
    }

    [Fact]
    public async Task Register_DuplicateEmail_IsRejected()
    {
        using var db = TestHelpers.NewDb();
        var auth = NewService(db);
        var request = new RegisterRequest { Email = "a@b.co", Password = "Password123!", ShopName = "Shop" };
        await auth.RegisterAsync(request);

        var second = await auth.RegisterAsync(request);

        Assert.False(second.Success);
    }

    [Fact]
    public async Task Register_DoesNotStorePlainTextPassword()
    {
        using var db = TestHelpers.NewDb();
        var auth = NewService(db);
        await auth.RegisterAsync(new RegisterRequest { Email = "a@b.co", Password = "Password123!", ShopName = "Shop" });

        var stored = db.Users.Single();

        Assert.NotEqual("Password123!", stored.PasswordHash);
        Assert.StartsWith("$2", stored.PasswordHash); // bcrypt
    }

    [Fact]
    public async Task Login_WithCorrectPassword_Succeeds()
    {
        using var db = TestHelpers.NewDb();
        var auth = NewService(db);
        await auth.RegisterAsync(new RegisterRequest { Email = "a@b.co", Password = "Password123!", ShopName = "Shop" });

        var result = await auth.LoginAsync(new LoginRequest { Email = "A@B.co", Password = "Password123!" });

        Assert.True(result.Success);
    }

    [Theory]
    [InlineData("a@b.co", "wrong-password")]
    [InlineData("nobody@b.co", "Password123!")]
    public async Task Login_WithBadCredentials_FailsWithSameMessage(string email, string password)
    {
        using var db = TestHelpers.NewDb();
        var auth = NewService(db);
        await auth.RegisterAsync(new RegisterRequest { Email = "a@b.co", Password = "Password123!", ShopName = "Shop" });

        var result = await auth.LoginAsync(new LoginRequest { Email = email, Password = password });

        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.Error);
    }
}
