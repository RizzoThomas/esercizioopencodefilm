using FilmAPI.Data;
using FilmAPI.DTO.Auth;
using FilmAPI.Model;
using FilmAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FilmAPI.Tests.Unit.Auth;

public class AuthServiceTests : IDisposable
{
    private readonly FilmDbContext _context;
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<FilmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new FilmDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "this-is-a-super-secret-key-min-32-chars-long",
                ["Jwt:Issuer"] = "FilmAPI",
                ["Jwt:Audience"] = "CineBase.Web",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        _jwtService = new JwtService(config);
        _authService = new AuthService(_context, _jwtService, config);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUser()
    {
        // Arrange
        var dto = new RegisterRequestDTO(
            "test@test.com",
            "password123",
            "Mario",
            "Rossi",
            null,
            null
        );

        // Act
        var (success, user, error) = await _authService.RegisterAsync(dto);

        // Assert
        success.Should().BeTrue();
        user.Should().NotBeNull();
        user!.Email.Should().Be("test@test.com");
        user.Nome.Should().Be("Mario");
        user.Ruolo.Should().Be("User");

        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@test.com");
        dbUser.Should().NotBeNull();
        dbUser!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ReturnsError()
    {
        // Arrange
        var dto = new RegisterRequestDTO(
            "duplicate@test.com",
            "password123",
            "Test",
            "User",
            null,
            null
        );

        await _authService.RegisterAsync(dto);

        // Act
        var (success, user, error) = await _authService.RegisterAsync(dto);

        // Assert
        success.Should().BeFalse();
        user.Should().BeNull();
        error.Should().Contain("esiste già");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequestDTO(
            "login@test.com",
            "password123",
            "Login",
            "Test",
            null,
            null
        ));

        var loginDto = new LoginRequestDTO("login@test.com", "password123");

        // Act
        var (success, response, error) = await _authService.LoginAsync(loginDto);

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.AccessToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.User.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequestDTO(
            "wrongpass@test.com",
            "password123",
            "Wrong",
            "Pass",
            null,
            null
        ));

        var loginDto = new LoginRequestDTO("wrongpass@test.com", "wrongpassword");

        // Act
        var (success, response, error) = await _authService.LoginAsync(loginDto);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Email o password non validi");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginRequestDTO("nonexistent@test.com", "password123");

        // Act
        var (success, response, error) = await _authService.LoginAsync(loginDto);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Email o password non validi");
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsUnauthorized()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequestDTO(
            "inactive@test.com",
            "password123",
            "Inactive",
            "User",
            null,
            null
        ));

        var user = await _context.Users.FirstAsync(u => u.Email == "inactive@test.com");
        user.IsActive = false;
        await _context.SaveChangesAsync();

        var loginDto = new LoginRequestDTO("inactive@test.com", "password123");

        // Act
        var (success, response, error) = await _authService.LoginAsync(loginDto);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Contain("Email o password non validi");
    }

    [Fact]
    public async Task LogoutAsync_RevokesRefreshTokens()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequestDTO(
            "logout@test.com",
            "password123",
            "Logout",
            "Test",
            null,
            null
        ));

        var loginResponse = await _authService.LoginAsync(new LoginRequestDTO("logout@test.com", "password123"));
        var userId = loginResponse.Data!.User.Id;

        // Act
        await _authService.LogoutAsync(userId);

        // Assert
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        activeTokens.Should().BeEmpty();
    }
}
