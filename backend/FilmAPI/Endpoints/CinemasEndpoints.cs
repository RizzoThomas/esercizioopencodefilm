using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class CinemasEndpoints
{
    public static void MapCinemasEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/cinemas");

        // GET - Pubblico
        group.MapGet("", async (ICinemaService service) =>
            await service.GetAllAsync())
            .AllowAnonymous();

        group.MapGet("/{id}", async (int id, ICinemaService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();

        // POST/PUT/DELETE - Solo Admin (PowerUser può solo leggere)
        group.MapPost("", async (CinemaCreateDTO dto, ICinemaService service) =>
        {
            var result = await service.CreateAsync(dto);
            return Results.Created($"/cinemas/{result.Id}", result);
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));

        group.MapPut("/{id}", async (int id, CinemaUpdateDTO dto, ICinemaService service) =>
        {
            var result = await service.UpdateAsync(id, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));

        group.MapDelete("/{id}", async (int id, ICinemaService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
