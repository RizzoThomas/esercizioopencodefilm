using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FilmAPI.Endpoints;

public static class OfferteEndpoints
{
    public static void MapOfferteEndpoints(this WebApplication app)
    {
        var offerteGroup = app.MapGroup("/offerte");

        offerteGroup.MapGet("/", async (FilmDbContext db, int? cinemaId) =>
        {
            var query = db.Offerte
                .AsNoTracking()
                .Where(o => o.Attiva);

            if (cinemaId.HasValue && cinemaId.Value > 0)
                query = query.Where(o => o.CinemaId == cinemaId.Value);

            var offerte = await query
                .OrderBy(o => o.Prezzo)
                .ThenBy(o => o.Id)
                .Select(o => new
                {
                    id = o.Id,
                    nome = o.Nome,
                    descrizione = o.Descrizione,
                    tipo = o.Tipo,
                    prezzo = o.Prezzo,
                    prezzoOriginale = o.PrezzoOriginale,
                    scontoPercentuale = o.ScontoPercentuale,
                    inEvidenza = o.InEvidenza,
                    numeroBiglietti = o.NumeroBiglietti,
                    includePopcorn = o.IncludePopcorn,
                    cinemaId = o.CinemaId,
                    cinemaNome = o.Cinema != null ? o.Cinema.Nome : null
                })
                .ToListAsync();

            return Results.Ok(offerte);
        });

        offerteGroup.MapPost("/{id}/acquista", async (
            int id,
            AcquistaOffertaRequest req,
            ClaimsPrincipal user,
            FilmDbContext db,
            ISeatHoldService seatHoldService,
            ICheckoutService checkoutService) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            if (req.ShowId <= 0)
                return Results.BadRequest("ShowId obbligatorio.");

            var offer = await db.Offerte.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id && o.Attiva);
            if (offer is null)
                return Results.NotFound("Offerta non trovata.");

            var show = await db.Shows
                .Include(s => s.Sala)
                .Include(s => s.Film)
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.Id == req.ShowId);

            if (show is null || show.Sala is null)
                return Results.NotFound("Show non trovato.");

            if (offer.NumeroBiglietti <= 0)
                return Results.Conflict("Offerta non valida.");

            var seatIds = await SelectAvailableSeatIdsAsync(db, show.Id, show.SalaId, offer.NumeroBiglietti);
            if (seatIds is null)
                return Results.Conflict("Posti insufficienti per questa offerta.");

            var hold = await seatHoldService.CreateHoldAsync(show.Id, userId, seatIds);
            if (hold.Conflitti.Count > 0 || string.IsNullOrWhiteSpace(hold.HoldToken))
                return Results.Conflict(hold);

            var createdOrder = await checkoutService.CreateOrdineAsync(userId, new CreateOrdineRequestDTO
            {
                HoldToken = hold.HoldToken
            });

            var order = await db.Ordini
                .Include(o => o.Show)!.ThenInclude(s => s!.Film)
                .Include(o => o.Show)!.ThenInclude(s => s!.Cinema)
                .Include(o => o.Show)!.ThenInclude(s => s!.Sala)
                .Include(o => o.Biglietti)
                    .ThenInclude(b => b.SalaPosto)
                .FirstOrDefaultAsync(o => o.Id == createdOrder.Id && o.UserId == userId);

            if (order is null)
                return Results.NotFound("Ordine non trovato.");

            await FinalizeOfferOrderAsync(db, order, offer, seatIds);

            var summary = await checkoutService.GetOrdineByIdAsync(order.Id, userId);
            return summary is null ? Results.NotFound() : Results.Ok(summary);
        }).RequireAuthorization("Authenticated");
    }

    private static async Task<List<int>?> SelectAvailableSeatIdsAsync(FilmDbContext db, int showId, int salaId, int requiredCount)
    {
        var now = DateTime.UtcNow;

        var seats = await db.SalaPosti
            .Where(p => p.SalaId == salaId && p.IsAttivo)
            .OrderBy(p => p.Settore)
            .ThenBy(p => p.Fila)
            .ThenBy(p => p.Numero)
            .ToListAsync();

        var stati = await db.ShowPostiStato
            .Where(s => s.ShowId == showId)
            .ToListAsync();

        var unavailable = stati
            .Where(s => s.Stato == ShowPostoState.Sold
                || (s.Stato == ShowPostoState.Hold && s.ScadeAtUtc.HasValue && s.ScadeAtUtc.Value > now))
            .Select(s => s.SalaPostoId)
            .ToHashSet();

        var available = seats
            .Where(s => !unavailable.Contains(s.Id))
            .Select(s => s.Id)
            .Take(requiredCount)
            .ToList();

        return available.Count == requiredCount ? available : null;
    }

    private static async Task FinalizeOfferOrderAsync(FilmDbContext db, Ordine order, Offerta offer, List<int> seatIds)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        var seatStates = await db.ShowPostiStato
            .Include(s => s.SalaPosto)
            .Where(s => s.OrdineId == order.Id)
            .OrderBy(s => s.SalaPostoId)
            .ToListAsync();

        if (seatStates.Count != seatIds.Count)
            throw new InvalidOperationException("Numero posti ordinati non coerente con l'offerta.");

        var unitPrice = offer.NumeroBiglietti <= 0
            ? 0m
            : offer.Prezzo / offer.NumeroBiglietti;

        order.NumeroBiglietti = offer.NumeroBiglietti;
        order.TotaleLordo = offer.Prezzo;
        order.ImportoCredito = 0m;
        order.ImportoCarta = offer.Prezzo;
        order.StripePaymentIntentId = null;
        order.StripeCheckoutSessionId = null;
        order.CheckoutExpiresAtUtc = null;
        order.CheckoutCompletedAtUtc = now;
        order.PaidAtUtc = now;
        order.Stato = OrdineState.Paid;

        foreach (var seatState in seatStates)
        {
            seatState.Stato = ShowPostoState.Sold;
            seatState.HoldToken = null;
            seatState.ScadeAtUtc = null;
            seatState.UpdatedAtUtc = now;
        }

        var existingCodes = new HashSet<string>();
        foreach (var seatState in seatStates)
        {
            var code = await GenerateUniqueTicketCodeAsync(db, existingCodes);
            existingCodes.Add(code);

            db.Biglietti.Add(new Biglietto
            {
                OrdineId = order.Id,
                ShowId = order.ShowId,
                SalaPostoId = seatState.SalaPostoId,
                UserId = order.UserId,
                CodiceBiglietto = code,
                BarcodeValue = code,
                PrezzoBase = unitPrice,
                Supplemento = 0m,
                PrezzoTotale = unitPrice,
                Stato = BigliettoState.Issued
            });
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task<string> GenerateUniqueTicketCodeAsync(FilmDbContext db, HashSet<string> inMemoryCodes)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = $"CB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

            if (inMemoryCodes.Contains(code))
                continue;

            var exists = await db.Biglietti.AnyAsync(b => b.CodiceBiglietto == code);
            if (!exists)
                return code;
        }

        throw new InvalidOperationException("Impossibile generare un codice ticket univoco per l'offerta.");
    }
}
