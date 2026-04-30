using System.Net;
using System.Net.Http.Json;
using FilmAPI.DTO;

namespace FilmAPI.Tests.Integration;

public class CategoriaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CategoriaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CAT1_GetCategorie_ReturnsAllCategories()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/categorie");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<CategoriaDTO>>();
        Assert.NotNull(payload);
    }

    [Fact]
    public async Task CAT2_CreateCategoria_ReturnsCreated_WithValidData()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var request = new CategoriaCreateDTO { Nome = "Azione" };

        var response = await client.PostAsJsonAsync("/categorie", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CategoriaDTO>();
        Assert.NotNull(payload);
        Assert.True(payload.Id > 0);
        Assert.Equal("Azione", payload.Nome);
    }

    [Fact]
    public async Task CAT3_CreateCategoria_ReturnsConflict_WhenDuplicateName()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var request = new CategoriaCreateDTO { Nome = "Horror" };
        await client.PostAsJsonAsync("/categorie", request);

        var response = await client.PostAsJsonAsync("/categorie", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CAT4_UpdateCategoria_UpdatesName_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var createResponse = await client.PostAsJsonAsync("/categorie", new CategoriaCreateDTO { Nome = "Thriller" });
        var created = await createResponse.Content.ReadFromJsonAsync<CategoriaDTO>();
        Assert.NotNull(created);

        var updateRequest = new CategoriaUpdateDTO { Nome = "Thriller Avventuroso" };
        var response = await client.PutAsJsonAsync($"/categorie/{created.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CategoriaDTO>();
        Assert.NotNull(payload);
        Assert.Equal("Thriller Avventuroso", payload.Nome);
    }

    [Fact]
    public async Task CAT5_DeleteCategoria_DeletesEntity_WhenExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreatePowerUserClient();

        var createResponse = await client.PostAsJsonAsync("/categorie", new CategoriaCreateDTO { Nome = "Documentario" });
        var created = await createResponse.Content.ReadFromJsonAsync<CategoriaDTO>();
        Assert.NotNull(created);

        var deleteResponse = await client.DeleteAsync($"/categorie/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/categorie/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
