using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class RegistiEndpoints
{
    public static void MapRegistiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/registi");

        // GET - Pubblico (anche non autenticato)
        group.MapGet("", async (IRegistaService service) =>
            await service.GetAllAsync())
            .AllowAnonymous();

        group.MapGet("/{id}", async (int id, IRegistaService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();

        group.MapGet("/{id}/films", async (int id, IRegistaService service) =>
        {
            var result = await service.GetFilmsByRegistaIdAsync(id);
            return Results.Ok(result);
        }).AllowAnonymous();

        // POST/PUT/DELETE - Richiede Admin o PowerUser
        group.MapPost("", async (RegistaCreateDTO dto, IRegistaService service) =>
        {
            try
            {
                var result = await service.CreateAsync(dto);
                return Results.Created($"/registi/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.PowerUser)));

        group.MapPut("/{id}", async (int id, RegistaUpdateDTO dto, IRegistaService service) =>
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

        group.MapDelete("/{id}", async (int id, IRegistaService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));

        group.MapPost("/{id}/films", async (int id, FilmCreateDTO dto, IFilmService filmService) =>
        {
            try
            {
                var filmDto = new FilmCreateDTO
                {
                    Titolo = dto.Titolo,
                    DataProduzione = dto.DataProduzione,
                    RegistaId = id,
                    Durata = dto.Durata,
                    CopertinaPath = dto.CopertinaPath,
                    FilmatoPath = dto.FilmatoPath
                };
                var result = await filmService.CreateAsync(filmDto);
                return Results.Created($"/films/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.PowerUser)));
    }
}
