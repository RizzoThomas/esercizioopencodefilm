using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class FilmsEndpoints
{
    public static void MapFilmsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/films");

        // GET - Pubblico
        group.MapGet("", async (IFilmService service) =>
            await service.GetAllAsync())
            .AllowAnonymous();

        group.MapGet("/{id}", async (int id, IFilmService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();

        // POST/PUT/DELETE - Richiede Admin o PowerUser
        group.MapPost("", async (FilmCreateDTO dto, IFilmService service) =>
        {
            try
            {
                var result = await service.CreateAsync(dto);
                return Results.Created($"/films/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.PowerUser)));

        group.MapPut("/{id}", async (int id, FilmUpdateDTO dto, IFilmService service) =>
        {
            try
            {
                var result = await service.UpdateAsync(id, dto);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.PowerUser)));

        group.MapDelete("/{id}", async (int id, IFilmService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
