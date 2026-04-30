using System.Net;
using System.Net.Http.Json;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task A1_Register_ReturnsAuthResponse_WithValidData()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var request = new RegisterRequestDTO
        {
            Email = "user@test.com",
            Password = "Password123!",
            Nome = "Mario",
            Cognome = "Rossi",
            Telefono = "1234567890"
        };

        var response = await client.PostAsJsonAsync("/auth/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDTO>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.AccessToken);
        Assert.NotEmpty(payload.RefreshToken);
        Assert.Equal("user@test.com", payload.User.Email);
        Assert.Equal("Mario", payload.User.Nome);
        Assert.Equal("Rossi", payload.User.Cognome);
        Assert.Equal("User", payload.User.Ruolo);
    }

    [Fact]
    public async Task A2_Register_ReturnsConflict_WhenEmailAlreadyExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var request = new RegisterRequestDTO
        {
            Email = "duplicate@test.com",
            Password = "Password123!",
            Nome = "Mario",
            Cognome = "Rossi"
        };

        await client.PostAsJsonAsync("/auth/register", request);
        var response = await client.PostAsJsonAsync("/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task A3_Login_ReturnsAuthResponse_WithValidCredentials()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var registerRequest = new RegisterRequestDTO
        {
            Email = "login@test.com",
            Password = "Password123!",
            Nome = "Luigi",
            Cognome = "Verdi"
        };
        await client.PostAsJsonAsync("/auth/register", registerRequest);

        var loginRequest = new LoginRequestDTO
        {
            Email = "login@test.com",
            Password = "Password123!"
        };

        var response = await client.PostAsJsonAsync("/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDTO>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.AccessToken);
        Assert.NotEmpty(payload.RefreshToken);
        Assert.Equal("login@test.com", payload.User.Email);
    }

    [Fact]
    public async Task A4_Login_ReturnsUnauthorized_WithInvalidCredentials()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var registerRequest = new RegisterRequestDTO
        {
            Email = "wrong@test.com",
            Password = "Password123!",
            Nome = "Test",
            Cognome = "User"
        };
        await client.PostAsJsonAsync("/auth/register", registerRequest);

        var loginRequest = new LoginRequestDTO
        {
            Email = "wrong@test.com",
            Password = "WrongPassword!"
        };

        var response = await client.PostAsJsonAsync("/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A5_Refresh_ReturnsNewTokens_AndRevokesOldRefreshToken()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var registerRequest = new RegisterRequestDTO
        {
            Email = "refresh@test.com",
            Password = "Password123!",
            Nome = "Refresh",
            Cognome = "Test"
        };
        var registerResponse = await client.PostAsJsonAsync("/auth/register", registerRequest);
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDTO>();
        Assert.NotNull(registerPayload);

        var oldRefreshToken = registerPayload.RefreshToken;

        var refreshRequest = new RefreshTokenRequestDTO
        {
            RefreshToken = oldRefreshToken
        };

        var refreshResponse = await client.PostAsJsonAsync("/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDTO>();
        Assert.NotNull(refreshPayload);
        Assert.NotEmpty(refreshPayload.AccessToken);
        Assert.NotEmpty(refreshPayload.RefreshToken);
        Assert.NotEqual(oldRefreshToken, refreshPayload.RefreshToken);

        var reuseResponse = await client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDTO { RefreshToken = oldRefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task A6_Refresh_ReturnsUnauthorized_WithInvalidToken()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var refreshRequest = new RefreshTokenRequestDTO
        {
            RefreshToken = "invalid-token-that-does-not-exist"
        };

        var response = await client.PostAsJsonAsync("/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A7_Logout_RevokesRefreshToken()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var registerRequest = new RegisterRequestDTO
        {
            Email = "logout@test.com",
            Password = "Password123!",
            Nome = "Logout",
            Cognome = "Test"
        };
        var registerResponse = await client.PostAsJsonAsync("/auth/register", registerRequest);
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDTO>();
        Assert.NotNull(registerPayload);

        var refreshToken = registerPayload.RefreshToken;

        var logoutRequest = new RefreshTokenRequestDTO
        {
            RefreshToken = refreshToken
        };

        var logoutClient = _factory.CreateUserClient();
        var logoutResponse = await logoutClient.PostAsJsonAsync("/auth/logout", logoutRequest);

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var reuseResponse = await client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDTO { RefreshToken = refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task A8_Me_ReturnsUserInfo_WhenAuthenticated()
    {
        await _factory.ResetDatabaseAsync(seed: async db =>
        {
            db.Users.Add(new User
            {
                Email = "me@test.com",
                PasswordHash = "hash",
                Nome = "MeUser",
                Cognome = "Test",
                Ruolo = UserRole.User,
                DataRegistrazione = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateAuthenticatedClient("User", userId: 1, email: "me@test.com", nome: "MeUser");

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<UserInfoDTO>();
        Assert.NotNull(payload);
        Assert.Equal("me@test.com", payload.Email);
        Assert.Equal("MeUser", payload.Nome);
    }
}
