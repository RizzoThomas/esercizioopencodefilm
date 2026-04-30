using System.Net;
using System.Net.Http.Json;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Tests.Integration;

public class RbacIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RbacIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RB1_Anonymous_OnProtectedEndpoint_ReturnsUnauthorized()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/registi/", new { Nome = "Test", Cognome = "User", Nazionalita = "IT" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RB2_User_OnAdminOnlyEndpoint_ReturnsForbidden()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateUserClient();

        var response = await client.GetAsync("/admin/utenti");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RB3_User_OnPowerUserOrAdminEndpoint_ReturnsForbidden()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateUserClient();

        var response = await client.PostAsJsonAsync("/registi/", new RegistaCreateDTO
        {
            Nome = "Test",
            Cognome = "User",
            Nazionalita = "IT"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RB4_PowerUser_OnAdminOnlyEndpoint_ReturnsForbidden()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var response = await client.GetAsync("/admin/utenti");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RB5_PowerUser_OnPowerUserOrAdminEndpoint_ReturnsSuccess()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var response = await client.PostAsJsonAsync("/registi/", new RegistaCreateDTO
        {
            Nome = "Test",
            Cognome = "Director",
            Nazionalita = "IT"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task RB6_Admin_OnAdminOnlyEndpoint_ReturnsSuccess()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/admin/utenti");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RB7_Admin_OnPowerUserOrAdminEndpoint_ReturnsSuccess()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync("/registi/", new RegistaCreateDTO
        {
            Nome = "Admin",
            Cognome = "Director",
            Nazionalita = "US"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task RB8_Anonymous_OnPublicGetEndpoint_ReturnsSuccess()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/films/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
