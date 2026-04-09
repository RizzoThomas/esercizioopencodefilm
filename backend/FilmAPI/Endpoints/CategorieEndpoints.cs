using FilmAPI.DTO.Categoria;
using FilmAPI.Model;
using FilmAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace FilmAPI.Endpoints;

public static class CategorieEndpoints
{
    public static void MapCategorieEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/categorie");

        // GET - Pubblico
        group.MapGet("", async (ICategoriaService service) =>
            await service.GetAllAsync())
            .AllowAnonymous();

        group.MapGet("/{id}", async (int id, ICategoriaService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();

        // POST/PUT/DELETE - Solo Admin
        group.MapPost("", async (CategoriaCreateDTO dto, ICategoriaService service) =>
        {
            var result = await service.CreateAsync(dto);
            return Results.Created($"/categorie/{result.Id}", result);
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));

        group.MapPut("/{id}", async (int id, CategoriaCreateDTO dto, ICategoriaService service) =>
        {
            var result = await service.UpdateAsync(id, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));

        group.MapDelete("/{id}", async (int id, ICategoriaService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));

        // Film-Categoria relationships
        group.MapGet("/film/{filmId}", async (int filmId, ICategoriaService service) =>
        {
            var result = await service.GetCategorieByFilmIdAsync(filmId);
            return Results.Ok(result);
        }).AllowAnonymous();

        group.MapPost("/film/{filmId}/{categoriaId}", async (int filmId, int categoriaId, ICategoriaService service) =>
        {
            try
            {
                await service.AddCategoriaToFilmAsync(filmId, categoriaId);
                return Results.Ok();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.PowerUser)));

        group.MapDelete("/film/{filmId}/{categoriaId}", async (int filmId, int categoriaId, ICategoriaService service) =>
        {
            await service.RemoveCategoriaFromFilmAsync(filmId, categoriaId);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.PowerUser)));
    }
}
