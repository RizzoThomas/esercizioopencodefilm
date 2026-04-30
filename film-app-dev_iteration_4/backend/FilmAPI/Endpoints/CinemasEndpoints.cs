using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class CinemasEndpoints
{
    public static void MapCinemasEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/cinemas");

        group.MapGet("", async (int? page, int? pageSize, string? search, ICinemaService service) =>
        {
            if (!page.HasValue && !pageSize.HasValue && string.IsNullOrWhiteSpace(search))
            {
                return Results.Ok(await service.GetAllAsync());
            }

            var result = await service.GetPagedAsync(page ?? 1, pageSize ?? 10, search);
            return Results.Ok(result);
        })
            .AllowAnonymous();

        group.MapGet("/{id}", async (int id, ICinemaService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();

        group.MapPost("", async (CinemaCreateDTO dto, ICinemaService service) =>
        {
            var result = await service.CreateAsync(dto);
            return Results.Created($"/cinemas/{result.Id}", result);
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/{id}", async (int id, CinemaUpdateDTO dto, ICinemaService service) =>
        {
            var result = await service.UpdateAsync(id, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        group.MapDelete("/{id}", async (int id, ICinemaService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("AdminOnly");
    }
}
