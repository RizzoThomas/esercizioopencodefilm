using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class ProiezioniEndpoints
{
    public static void MapProiezioniEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/proiezioni");

        // GET - Pubblico
        group.MapGet("", async (IProiezioneService service) =>
            await service.GetAllAsync())
            .AllowAnonymous();

        group.MapGet("/{id}", async (int id, IProiezioneService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();

        // POST/PUT/DELETE - Richiede Admin o PowerUser
        group.MapPost("", async (ProiezioneCreateDTO dto, IProiezioneService service) =>
        {
            try
            {
                var result = await service.CreateAsync(dto);
                return Results.Created($"/proiezioni/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.PowerUser)));

        group.MapPut("/{id}", async (int id, ProiezioneUpdateDTO dto, IProiezioneService service) =>
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
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin), nameof(UserRole.PowerUser)));

        group.MapDelete("/{id}", async (int id, IProiezioneService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
