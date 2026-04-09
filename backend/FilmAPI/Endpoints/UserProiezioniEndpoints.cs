using FilmAPI.DTO.UserProiezione;
using FilmAPI.Extensions;
using FilmAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace FilmAPI.Endpoints;

public static class UserProiezioniEndpoints
{
    public static void MapUserProiezioniEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/me/proiezioni")
            .WithTags("Area Personale - Proiezioni Salvate")
            .RequireAuthorization();

        // GET /me/proiezioni - Proiezioni salvate dall'utente corrente
        group.MapGet("", async (HttpContext httpContext, IUserProiezioneService service) =>
        {
            var userId = httpContext.User.GetUserId();
            var result = await service.GetByUserIdAsync(userId);
            return Results.Ok(result);
        })
        .WithName("GetSavedProiezioni")
        .Produces<List<UserProiezioneDTO>>(200);

        // GET /me/proiezioni/{id} - Dettaglio proiezione salvata
        group.MapGet("/{id}", async (int id, HttpContext httpContext, IUserProiezioneService service) =>
        {
            var userId = httpContext.User.GetUserId();
            var result = await service.GetByIdAsync(id, userId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetSavedProiezioneById")
        .Produces<UserProiezioneDTO>(200)
        .Produces(404);

        // POST /me/proiezioni - Salva una proiezione
        group.MapPost("", async (UserProiezioneCreateDTO dto, HttpContext httpContext, IUserProiezioneService service) =>
        {
            try
            {
                var userId = httpContext.User.GetUserId();
                var result = await service.CreateAsync(userId, dto);
                return Results.Created($"/me/proiezioni/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { Message = ex.Message });
            }
        })
        .WithName("SaveProiezione")
        .Produces<UserProiezioneDTO>(201)
        .Produces(400)
        .Produces(409);

        // DELETE /me/proiezioni/{id} - Rimuove dai salvati
        group.MapDelete("/{id}", async (int id, HttpContext httpContext, IUserProiezioneService service) =>
        {
            var userId = httpContext.User.GetUserId();
            var result = await service.DeleteAsync(id, userId);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteSavedProiezione")
        .Produces(204)
        .Produces(404);
    }
}
