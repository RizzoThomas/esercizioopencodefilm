using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class ProiezioniEndpoints
{
    public static void MapProiezioniEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/proiezioni");

        group.MapGet("", async (int? page, int? pageSize, string? search, IProiezioneService service) =>
        {
            if (!page.HasValue && !pageSize.HasValue && string.IsNullOrWhiteSpace(search))
            {
                return Results.Ok(await service.GetAllAsync());
            }

            var result = await service.GetPagedAsync(page ?? 1, pageSize ?? 10, search);
            return Results.Ok(result);
        })
            .AllowAnonymous();

        group.MapGet("/{id}", async (int id, IProiezioneService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();

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
        }).RequireAuthorization("PowerUserOrAdmin");

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
        }).RequireAuthorization("PowerUserOrAdmin");

        group.MapDelete("/{id}", async (int id, IProiezioneService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("PowerUserOrAdmin");
    }
}
