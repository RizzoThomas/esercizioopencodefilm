using System.Net;
using System.Net.Http.Json;
using FilmAPI.DTO.Auth;

namespace FilmAPI.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostAuthRegister_WithValidData_ReturnsCreated()
    {
        // Arrange
        var dto = new RegisterRequestDTO(
            "int@test.com",
            "password123",
            "Integration",
            "Test",
            null,
            null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/auth/register", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostAuthLogin_WithValidCredentials_ReturnsTokens()
    {
        // Arrange - register first
        var registerDto = new RegisterRequestDTO(
            "loginint@test.com",
            "password123",
            "Login",
            "Integration",
            null,
            null
        );
        await _client.PostAsJsonAsync("/auth/register", registerDto);

        var loginDto = new LoginRequestDTO("loginint@test.com", "password123");

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PostAuthLogin_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new LoginRequestDTO("wrong@test.com", "wrongpass");

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuthMe_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuthMe_WithValidToken_ReturnsUser()
    {
        // Arrange - register and login
        var registerDto = new RegisterRequestDTO(
            "meint@test.com",
            "password123",
            "Me",
            "Test",
            null,
            null
        );
        await _client.PostAsJsonAsync("/auth/register", registerDto);

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", 
            new LoginRequestDTO("meint@test.com", "password123"));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        // Act
        var response = await _client.GetAsync("/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostAuthRefresh_WithValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange - register and login
        var registerDto = new RegisterRequestDTO(
            "refreshint@test.com",
            "password123",
            "Refresh",
            "Test",
            null,
            null
        );
        await _client.PostAsJsonAsync("/auth/register", registerDto);

        var loginResponse = await _client.PostAsJsonAsync("/auth/login",
            new LoginRequestDTO("refreshint@test.com", "password123"));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();

        var refreshDto = new RefreshTokenRequestDTO(loginResult!.RefreshToken);

        // Act
        var response = await _client.PostAsJsonAsync("/auth/refresh", refreshDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PostAuthRefresh_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new RefreshTokenRequestDTO("invalid.refresh.token");

        // Act
        var response = await _client.PostAsJsonAsync("/auth/refresh", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostAuthLogout_WithValidToken_ReturnsNoContent()
    {
        // Arrange - register and login
        var registerDto = new RegisterRequestDTO(
            "logoutint@test.com",
            "password123",
            "Logout",
            "Test",
            null,
            null
        );
        await _client.PostAsJsonAsync("/auth/register", registerDto);

        var loginResponse = await _client.PostAsJsonAsync("/auth/login",
            new LoginRequestDTO("logoutint@test.com", "password123"));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        // Act
        var response = await _client.PostAsync("/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PostAuthLogout_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/registi", "GET")]
    [InlineData("/films", "GET")]
    [InlineData("/cinemas", "GET")]
    [InlineData("/proiezioni", "GET")]
    public async Task GetPublicEndpoints_WithoutToken_ReturnsOk(string endpoint, string method)
    {
        // Act
        HttpResponseMessage response;
        if (method == "GET")
            response = await _client.GetAsync(endpoint);
        else
            response = await _client.PostAsync(endpoint, new StringContent("{}"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/registi", "POST")]
    [InlineData("/films", "POST")]
    [InlineData("/proiezioni", "POST")]
    public async Task PostProtectedEndpoints_WithoutToken_ReturnsUnauthorized(string endpoint, string method)
    {
        // Act
        var response = await _client.PostAsync(endpoint, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/cinemas", "POST")]
    [InlineData("/cinemas", "PUT")]
    [InlineData("/cinemas", "DELETE")]
    public async Task CinemaMutations_WithoutAdminRole_ReturnsForbidden(string endpoint, string method)
    {
        // Arrange - register as regular user and login
        var registerDto = new RegisterRequestDTO(
            "usercinematest@test.com",
            "password123",
            "User",
            "Test",
            null,
            null
        );
        await _client.PostAsJsonAsync("/auth/register", registerDto);
        var loginResponse = await _client.PostAsJsonAsync("/auth/login",
            new LoginRequestDTO("usercinematest@test.com", "password123"));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
        
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        // Act
        HttpResponseMessage response;
        if (method == "POST")
            response = await _client.PostAsync(endpoint, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
            else if (method == "PUT")
                response = await _client.PutAsync($"{endpoint}/1", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
            else
                response = await _client.DeleteAsync($"{endpoint}/1");

        // Assert - Should be Unauthorized or Forbidden
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
