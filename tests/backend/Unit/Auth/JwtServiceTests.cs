using FilmAPI.Model;
using FilmAPI.Services;
using Microsoft.Extensions.Configuration;

namespace FilmAPI.Tests.Unit.Auth;

public class JwtServiceTests
{
    private readonly IJwtService _jwtService;

    public JwtServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "this-is-a-super-secret-key-min-32-chars-long",
                ["Jwt:Issuer"] = "FilmAPI",
                ["Jwt:Audience"] = "CineBase.Web",
                ["Jwt:AccessTokenExpirationMinutes"] = "15"
            })
            .Build();

        _jwtService = new JwtService(config);
    }

    [Fact]
    public void GenerateAccessToken_WithValidUser_ReturnsToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            Nome = "Test",
            Cognome = "User",
            Ruolo = UserRole.User
        };

        // Act
        var token = _jwtService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateAccessToken_WithAdminUser_ContainsAdminRole()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "admin@test.com",
            Nome = "Admin",
            Cognome = "User",
            Ruolo = UserRole.Admin
        };

        // Act
        var token = _jwtService.GenerateAccessToken(user);
        var principal = _jwtService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        var roleClaim = principal!.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role);
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be("Admin");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsToken()
    {
        // Act
        var token = _jwtService.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Length.Should().BeGreaterThan(20);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsPrincipal()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            Nome = "Test",
            Cognome = "User",
            Ruolo = UserRole.User
        };
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var principal = _jwtService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsNull()
    {
        // Act
        var principal = _jwtService.ValidateToken("invalid.token.string");

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_WithValidToken_ReturnsUserId()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            Email = "test@test.com",
            Nome = "Test",
            Cognome = "User",
            Ruolo = UserRole.User
        };
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var userId = _jwtService.GetUserIdFromToken(token);

        // Assert
        userId.Should().Be(42);
    }

    [Fact]
    public void GetUserRoleFromToken_WithValidToken_ReturnsRole()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "power@test.com",
            Nome = "Power",
            Cognome = "User",
            Ruolo = UserRole.PowerUser
        };
        var token = _jwtService.GenerateAccessToken(user);

        // Act
        var role = _jwtService.GetUserRoleFromToken(token);

        // Assert
        role.Should().Be("PowerUser");
    }
}
