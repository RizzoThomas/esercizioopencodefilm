using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class FilmsEndpoints
{
    public static void MapFilmsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/films");

        group.MapGet("", async (int? page, int? pageSize, string? search, string? filter, IFilmService service) =>
        {
            if (!page.HasValue && !pageSize.HasValue && string.IsNullOrWhiteSpace(search) && string.IsNullOrWhiteSpace(filter))
            {
                return Results.Ok(await service.GetAllAsync());
            }

            var result = await service.GetPagedAsync(page ?? 1, pageSize ?? 10, search, filter);
            return Results.Ok(result);
        }).AllowAnonymous();

        group.MapGet("/{id}", async (int id, IFilmService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();

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
        }).RequireAuthorization("PowerUserOrAdmin");

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
        }).RequireAuthorization("PowerUserOrAdmin");

        group.MapDelete("/{id}", async (int id, IFilmService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("PowerUserOrAdmin");
    }
}
