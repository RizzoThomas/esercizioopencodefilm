using FilmAPI.DTO;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class CategorieEndpoints
{
    public static void MapCategorieEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/categorie");

        group.MapGet("", async (ICategoriaService service) =>
            await service.GetAllAsync())
            .AllowAnonymous();

        group.MapGet("/{id}", async (int id, ICategoriaService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).AllowAnonymous();

        group.MapPost("", async (CategoriaCreateDTO dto, ICategoriaService service) =>
        {
            try
            {
                var result = await service.CreateAsync(dto);
                return Results.Created($"/categorie/{result.Id}", result);
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

        group.MapPut("/{id}", async (int id, CategoriaUpdateDTO dto, ICategoriaService service) =>
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

        group.MapDelete("/{id}", async (int id, ICategoriaService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("PowerUserOrAdmin");
    }
}
