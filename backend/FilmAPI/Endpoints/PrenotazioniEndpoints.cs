using FilmAPI.DTO.Prenotazione;
using FilmAPI.Extensions;
using FilmAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace FilmAPI.Endpoints;

public static class PrenotazioniEndpoints
{
    public static void MapPrenotazioniEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/me/prenotazioni")
            .WithTags("Area Personale - Prenotazioni")
            .RequireAuthorization();

        // GET /me/prenotazioni - Prenotazioni dell'utente
        group.MapGet("", async (HttpContext httpContext, IPrenotazioneService service) =>
        {
            var userId = httpContext.User.GetUserId();
            var result = await service.GetByUserIdAsync(userId);
            return Results.Ok(result);
        })
        .WithName("GetPrenotazioni")
        .Produces<List<PrenotazioneDTO>>(200);

        // GET /me/prenotazioni/{id} - Dettaglio prenotazione
        group.MapGet("/{id}", async (int id, HttpContext httpContext, IPrenotazioneService service) =>
        {
            var userId = httpContext.User.GetUserId();
            var result = await service.GetByIdAsync(id, userId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetPrenotazioneById")
        .Produces<PrenotazioneDTO>(200)
        .Produces(404);

        // POST /me/prenotazioni - Crea prenotazione
        group.MapPost("", async (PrenotazioneCreateDTO dto, HttpContext httpContext, IPrenotazioneService service) =>
        {
            try
            {
                var userId = httpContext.User.GetUserId();
                var result = await service.CreateAsync(userId, dto);
                return Results.Created($"/me/prenotazioni/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        })
        .WithName("CreatePrenotazione")
        .Produces<PrenotazioneDTO>(201)
        .Produces(400)
        .Produces(409);

        // GET /me/prenotazioni/disponibilita/{proiezioneId} - Disponibilita posti per proiezione
        group.MapGet("/disponibilita/{proiezioneId}", async (int proiezioneId, IPrenotazioneService service) =>
        {
            var result = await service.GetDisponibilitaAsync(proiezioneId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetPrenotazioneDisponibilita")
        .Produces<PrenotazioneDisponibilitaDTO>(200)
        .Produces(404);

        // PUT /me/prenotazioni/{id}/annulla - Annulla prenotazione
        group.MapPut("/{id}/annulla", async (int id, HttpContext httpContext, IPrenotazioneService service) =>
        {
            var userId = httpContext.User.GetUserId();
            var result = await service.AnnullaAsync(id, userId);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("AnnullaPrenotazione")
        .Produces(204)
        .Produces(404);

        // GET /prenotazioni/verifica/{codice} - Verifica prenotazione da codice (pubblico)
        app.MapGet("/prenotazioni/verifica/{codice}", async (string codice, IPrenotazioneService service) =>
        {
            var result = await service.GetByCodiceAsync(codice);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .AllowAnonymous()
        .WithTags("Prenotazioni")
        .WithName("VerificaPrenotazione")
        .Produces<PrenotazioneDTO>(200)
        .Produces(404);
    }
}
