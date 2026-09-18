using ClientManager.API.Controllers;
using ClientManager.API.DTOs;
using ClientManager.API.Models;
using ClientManager.API.Services;
using ClientManager.API.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ClientManager.API.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly (string Token, DateTime ExpiresAt) _fakeTokenResult;

    public AuthControllerTests()
    {
        _tokenServiceMock = new Mock<ITokenService>();
        _fakeTokenResult = ("fake.jwt.token", DateTime.UtcNow.AddHours(8));
        _tokenServiceMock
            .Setup(t => t.GenerateToken(It.IsAny<User>()))
            .Returns(_fakeTokenResult);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidRequest_Returns201WithToken()
    {
        using var db = DbContextFactory.Create();
        var controller = new AuthController(db, _tokenServiceMock.Object);
        var request = new RegisterRequest("newuser", "password123");

        var result = await controller.Register(request);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = created.Value.Should().BeOfType<AuthResponse>().Subject;
        response.Token.Should().Be("fake.jwt.token");
        response.Username.Should().Be("newuser");
    }

    [Fact]
    public async Task Register_SavesHashedPassword()
    {
        using var db = DbContextFactory.Create();
        var controller = new AuthController(db, _tokenServiceMock.Object);
        var request = new RegisterRequest("hashuser", "plaintext");

        await controller.Register(request);

        var saved = db.Users.Single(u => u.Username == "hashuser");
        saved.PasswordHash.Should().NotBe("plaintext");
        BCrypt.Net.BCrypt.Verify("plaintext", saved.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_Returns409()
    {
        using var db = DbContextFactory.Create();
        var controller = new AuthController(db, _tokenServiceMock.Object);

        await controller.Register(new RegisterRequest("existing", "password1"));
        var result = await controller.Register(new RegisterRequest("existing", "password2"));

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Register_CallsGenerateToken()
    {
        using var db = DbContextFactory.Create();
        var controller = new AuthController(db, _tokenServiceMock.Object);

        await controller.Register(new RegisterRequest("tokenuser", "password1"));

        _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Once);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        using var db = DbContextFactory.Create();
        db.Users.Add(new User
        {
            Username = "loginuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct")
        });
        await db.SaveChangesAsync();

        var controller = new AuthController(db, _tokenServiceMock.Object);
        var result = await controller.Login(new LoginRequest("loginuser", "correct"));

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AuthResponse>().Subject;
        response.Token.Should().Be("fake.jwt.token");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        using var db = DbContextFactory.Create();
        db.Users.Add(new User
        {
            Username = "loginuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct")
        });
        await db.SaveChangesAsync();

        var controller = new AuthController(db, _tokenServiceMock.Object);
        var result = await controller.Login(new LoginRequest("loginuser", "wrong"));

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithUnknownUsername_Returns401()
    {
        using var db = DbContextFactory.Create();
        var controller = new AuthController(db, _tokenServiceMock.Object);

        var result = await controller.Login(new LoginRequest("nobody", "password"));

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithValidCredentials_CallsGenerateToken()
    {
        using var db = DbContextFactory.Create();
        db.Users.Add(new User
        {
            Username = "tokenlogin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass123")
        });
        await db.SaveChangesAsync();

        var controller = new AuthController(db, _tokenServiceMock.Object);
        await controller.Login(new LoginRequest("tokenlogin", "pass123"));

        _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Once);
    }
}
