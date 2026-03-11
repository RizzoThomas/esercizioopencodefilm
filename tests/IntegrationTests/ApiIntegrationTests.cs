using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace IntegrationTests;

public class ApiIntegrationTests
{
    [Fact]
    public async Task Crud_Regista_Flow()
    {
        // Use WebApplicationFactory to host the API in-process for reliable tests
        using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<global::Program>();
        using var client = factory.CreateClient();

        var create = new { nome = "Test", cognome = "Regista", nazionalita = "IT" };
        var res = await client.PostAsJsonAsync("/registi", create);
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await res.Content.ReadFromJsonAsync<JsonElement>();
        int id = created.GetProperty("id").GetInt32();

        var get = await client.GetAsync($"/registi/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await client.GetAsync("/registi");
        list.StatusCode.Should().Be(HttpStatusCode.OK);

        var del = await client.DeleteAsync($"/registi/{id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
