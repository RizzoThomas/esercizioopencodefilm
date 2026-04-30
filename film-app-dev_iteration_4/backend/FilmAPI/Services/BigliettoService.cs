using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class BigliettoService : IBigliettoService
{
    private readonly FilmDbContext _db;

    public BigliettoService(FilmDbContext db)
    {
        _db = db;
    }

    public async Task EmitTicketsForOrderAsync(int orderId)
    {
        var ordine = await _db.Ordini
            .Include(o => o.Show)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (ordine is null)
            throw new KeyNotFoundException("Ordine non trovato.");

        if (ordine.Stato != OrdineState.Paid)
            throw new InvalidOperationException("I ticket possono essere emessi solo per ordini pagati.");

        var seatStates = await _db.ShowPostiStato
            .Include(s => s.SalaPosto)
            .Where(s => s.OrdineId == orderId)
            .OrderBy(s => s.SalaPosto!.Settore)
            .ThenBy(s => s.SalaPosto!.Fila)
            .ThenBy(s => s.SalaPosto!.Numero)
            .ToListAsync();

        if (seatStates.Count == 0)
            throw new InvalidOperationException("Ordine privo di posti associati.");

        if (seatStates.Any(s => s.Stato != ShowPostoState.Sold))
            throw new InvalidOperationException("I posti dell'ordine non risultano ancora venduti.");

        var existingSeatIds = await _db.Biglietti
            .Where(b => b.OrdineId == orderId)
            .Select(b => b.SalaPostoId)
            .ToListAsync();

        var existingSeatIdSet = existingSeatIds.ToHashSet();
        foreach (var seatState in seatStates.Where(s => !existingSeatIdSet.Contains(s.SalaPostoId)))
        {
            var ticketCode = await GenerateUniqueTicketCodeAsync();
            _db.Biglietti.Add(new Biglietto
            {
                OrdineId = ordine.Id,
                ShowId = ordine.ShowId,
                SalaPostoId = seatState.SalaPostoId,
                UserId = ordine.UserId,
                CodiceBiglietto = ticketCode,
                BarcodeValue = ticketCode,
                PrezzoBase = TicketPriceNormalizer.NormalizeUnitPrice(ordine.Show?.PrezzoBase ?? 0m),
                Supplemento = TicketPriceNormalizer.NormalizeUnitPrice(ordine.Show?.SupplementoSala ?? 0m),
                PrezzoTotale = TicketPriceNormalizer.NormalizeUnitPrice(ordine.Show?.PrezzoBase ?? 0m)
                    + TicketPriceNormalizer.NormalizeUnitPrice(ordine.Show?.SupplementoSala ?? 0m),
                Stato = BigliettoState.Issued
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<OrdineTicketDocumentDTO> GetOrderTicketDocumentAsync(int orderId)
    {
        var ordine = await _db.Ordini
            .Include(o => o.User)
            .Include(o => o.Show!)
            .ThenInclude(s => s!.Film)
            .Include(o => o.Show!)
            .ThenInclude(s => s!.Cinema)
            .Include(o => o.Show!)
            .ThenInclude(s => s!.Sala)
            .Include(o => o.Biglietti)
            .ThenInclude(b => b.SalaPosto)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (ordine is null)
            throw new KeyNotFoundException("Ordine non trovato.");

        if (ordine.Stato != OrdineState.Paid)
            throw new InvalidOperationException("Il PDF e disponibile solo per ordini pagati.");

        if (ordine.Biglietti.Count == 0)
            throw new InvalidOperationException("Nessun ticket disponibile per l'ordine richiesto.");

        var show = ordine.Show!;
        var user = ordine.User;

        return new OrdineTicketDocumentDTO
        {
            OrdineId = ordine.Id,
            CodiceOrdine = ordine.CodiceOrdine,
            UserId = ordine.UserId,
            RecipientEmail = user?.Email ?? string.Empty,
            RecipientName = BuildRecipientName(user),
            FilmTitolo = show.Film?.Titolo ?? string.Empty,
            CinemaNome = show.Cinema?.Nome ?? string.Empty,
            SalaNome = show.Sala?.Nome ?? $"Sala {show.Sala?.NumeroProgressivo}",
            StartAtUtc = show.StartAtUtc,
            NumeroBiglietti = ordine.NumeroBiglietti,
            TotaleLordo = TicketPriceNormalizer.NormalizeTotal(ordine.TotaleLordo, ordine.NumeroBiglietti),
            PaidAtUtc = ordine.PaidAtUtc,
            Tickets = ordine.Biglietti
                .OrderBy(b => b.SalaPosto?.Settore)
                .ThenBy(b => b.SalaPosto?.Fila)
                .ThenBy(b => b.SalaPosto?.Numero)
                .Select(b => MapToTicketPdfModel(ordine, b))
                .ToList()
        };
    }

    public async Task<TicketValidationLookupDTO?> GetTicketValidationLookupAsync(string code)
    {
        var normalizedCode = NormalizeCode(code);

        var ticket = await _db.Biglietti
            .Include(b => b.Ordine)
            .Include(b => b.Show!)
            .ThenInclude(s => s!.Film)
            .Include(b => b.Show!)
            .ThenInclude(s => s!.Cinema)
            .Include(b => b.Show!)
            .ThenInclude(s => s!.Sala)
            .Include(b => b.SalaPosto)
            .FirstOrDefaultAsync(b => b.CodiceBiglietto.ToUpper() == normalizedCode);

        return ticket is null ? null : MapToValidationLookup(ticket);
    }

    private static string BuildRecipientName(User? user)
    {
        if (user is null)
            return string.Empty;

        return string.Join(' ', new[] { user.Nome, user.Cognome }.Where(v => !string.IsNullOrWhiteSpace(v))).Trim();
    }

    private TicketPdfModel MapToTicketPdfModel(Ordine ordine, Biglietto ticket)
    {
        var show = ticket.Show ?? ordine.Show;
        var cinema = show?.Cinema;
        var sala = show?.Sala;
        var seat = ticket.SalaPosto;

        return new TicketPdfModel
        {
            TicketId = ticket.Id,
            OrdineId = ordine.Id,
            CodiceOrdine = ordine.CodiceOrdine,
            CodiceBiglietto = ticket.CodiceBiglietto,
            BarcodeValue = ticket.BarcodeValue,
            ValidationUrl = BuildValidationUrl(ticket.CodiceBiglietto),
            FilmTitolo = show?.Film?.Titolo ?? string.Empty,
            StartAtUtc = show?.StartAtUtc ?? default,
            CinemaNome = cinema?.Nome ?? string.Empty,
            CinemaCitta = cinema?.Citta ?? string.Empty,
            CinemaIndirizzo = cinema?.Indirizzo ?? string.Empty,
            CinemaCodiceLocale = cinema?.CodiceLocale,
            SalaNome = sala?.Nome ?? $"Sala {sala?.NumeroProgressivo}",
            SalaNumeroProgressivo = sala?.NumeroProgressivo ?? 0,
            Settore = seat?.Settore ?? string.Empty,
            Fila = seat?.Fila ?? 0,
            Numero = seat?.Numero ?? 0,
            PrezzoBase = TicketPriceNormalizer.NormalizeUnitPrice(ticket.PrezzoBase),
            Supplemento = TicketPriceNormalizer.NormalizeUnitPrice(ticket.Supplemento),
            PrezzoTotale = TicketPriceNormalizer.NormalizeUnitPrice(ticket.PrezzoTotale)
        };
    }

    private static TicketValidationLookupDTO MapToValidationLookup(Biglietto ticket)
    {
        var show = ticket.Show;
        var cinema = show?.Cinema;
        var sala = show?.Sala;
        var seat = ticket.SalaPosto;

        return new TicketValidationLookupDTO
        {
            TicketId = ticket.Id,
            OrdineId = ticket.OrdineId,
            ShowId = ticket.ShowId,
            CinemaId = show?.CinemaId ?? ticket.ValidatoCinemaId ?? 0,
            CodiceBiglietto = ticket.CodiceBiglietto,
            BarcodeValue = ticket.BarcodeValue,
            FilmTitolo = show?.Film?.Titolo ?? string.Empty,
            CinemaNome = cinema?.Nome ?? string.Empty,
            CinemaCitta = cinema?.Citta ?? string.Empty,
            CinemaIndirizzo = cinema?.Indirizzo ?? string.Empty,
            CinemaCodiceLocale = cinema?.CodiceLocale,
            SalaNome = sala?.Nome ?? $"Sala {sala?.NumeroProgressivo}",
            StartAtUtc = show?.StartAtUtc ?? default,
            Settore = seat?.Settore ?? string.Empty,
            Fila = seat?.Fila ?? 0,
            Numero = seat?.Numero ?? 0,
            PrezzoBase = TicketPriceNormalizer.NormalizeUnitPrice(ticket.PrezzoBase),
            Supplemento = TicketPriceNormalizer.NormalizeUnitPrice(ticket.Supplemento),
            PrezzoTotale = TicketPriceNormalizer.NormalizeUnitPrice(ticket.PrezzoTotale),
            Stato = ticket.Stato.ToString(),
            ValidatoAtUtc = ticket.ValidatoAtUtc,
            ValidatoDaUserId = ticket.ValidatoDaUserId,
            ValidatoCinemaId = ticket.ValidatoCinemaId
        };
    }

    private string BuildValidationUrl(string ticketCode)
    {
        var baseUrl = Environment.GetEnvironmentVariable("TICKET_VALIDATION_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";
            baseUrl = $"{frontendBaseUrl.TrimEnd('/')}/validazione-biglietti.html";
        }

        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{baseUrl}{separator}codice={Uri.EscapeDataString(ticketCode)}";
    }

    private async Task<string> GenerateUniqueTicketCodeAsync()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = $"CB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            var existsInMemory = _db.ChangeTracker
                .Entries<Biglietto>()
                .Any(e => e.State != EntityState.Deleted && e.Entity.CodiceBiglietto == code);

            if (existsInMemory)
                continue;

            var existsInDb = await _db.Biglietti.AnyAsync(b => b.CodiceBiglietto == code);
            if (!existsInDb)
                return code;
        }

        throw new InvalidOperationException("Impossibile generare un codice ticket univoco.");
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("CodiceBiglietto obbligatorio.");

        return code.Trim().ToUpperInvariant();
    }
}
