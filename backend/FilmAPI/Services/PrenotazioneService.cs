using FilmAPI.Data;
using FilmAPI.DTO.Prenotazione;
using FilmAPI.DTO.UserProiezione;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public interface IPrenotazioneService
{
    Task<List<PrenotazioneDTO>> GetByUserIdAsync(int userId);
    Task<PrenotazioneDTO?> GetByIdAsync(int id, int userId);
    Task<PrenotazioneDTO> CreateAsync(int userId, PrenotazioneCreateDTO dto);
    Task<bool> AnnullaAsync(int id, int userId);
    Task<PrenotazioneDTO?> GetByCodiceAsync(string codice);
    Task<PrenotazioneDisponibilitaDTO?> GetDisponibilitaAsync(int proiezioneId);
}

public class PrenotazioneService : IPrenotazioneService
{
    private readonly FilmDbContext _context;

    public PrenotazioneService(FilmDbContext context)
    {
        _context = context;
    }

    public async Task<List<PrenotazioneDTO>> GetByUserIdAsync(int userId)
    {
        var items = await _context.Prenotazioni
            .Where(p => p.UserId == userId)
            .Include(p => p.Proiezione)
            .ThenInclude(p => p.Film)
            .Include(p => p.Proiezione)
            .ThenInclude(p => p.Cinema)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return items.Select(p => MapPrenotazioneDTO(p)).ToList();
    }

    public async Task<PrenotazioneDTO?> GetByIdAsync(int id, int userId)
    {
        var prenotazione = await _context.Prenotazioni
            .Where(p => p.Id == id && p.UserId == userId)
            .Include(p => p.Proiezione)
            .ThenInclude(p => p.Film)
            .Include(p => p.Proiezione)
            .ThenInclude(p => p.Cinema)
            .FirstOrDefaultAsync();

        if (prenotazione == null) return null;

        return MapPrenotazioneDTO(prenotazione);
    }

    public async Task<PrenotazioneDTO> CreateAsync(int userId, PrenotazioneCreateDTO dto)
    {
        var proiezione = await _context.Proiezioni
            .Include(p => p.Film)
            .Include(p => p.Cinema)
            .FirstOrDefaultAsync(p => p.Id == dto.ProiezioneId);

        if (proiezione == null)
            throw new ArgumentException("Proiezione non trovata");

        if (proiezione.Data.Date < DateTime.UtcNow.Date)
            throw new InvalidOperationException("Non è possibile prenotare per una proiezione passata");

        var capienzaCinema = Math.Max(1, proiezione.Cinema?.CapienzaTotale ?? 120);

        var prenotazioniAttive = await _context.Prenotazioni
            .Where(p => p.ProiezioneId == dto.ProiezioneId && p.Stato != StatoPrenotazione.Annullata)
            .ToListAsync();

        var postiOccupati = prenotazioniAttive
            .SelectMany(GetPrenotazioneSeats)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tuttiIPosti = GenerateSeatLayout(capienzaCinema);

        if (dto.Posti is { Length: > 0 })
        {
            if (dto.Posti.Length != dto.NumeroPosti)
                throw new InvalidOperationException("Il numero dei posti selezionati non corrisponde al numero posti richiesto");

            var normalizedRequestedSeats = dto.Posti
                .Select(NormalizeSeatCode)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (normalizedRequestedSeats.Length != dto.NumeroPosti)
                throw new InvalidOperationException("I posti selezionati devono essere univoci e validi");

            var allSeatsSet = tuttiIPosti.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (normalizedRequestedSeats.Any(s => !allSeatsSet.Contains(s)))
                throw new InvalidOperationException("Uno o più posti selezionati non sono validi per questa sala");

            if (normalizedRequestedSeats.Any(s => postiOccupati.Contains(s)))
                throw new InvalidOperationException("Uno o più posti selezionati non sono più disponibili");

            var postiDisponibili = Math.Max(0, capienzaCinema - postiOccupati.Count);
            if (dto.NumeroPosti > postiDisponibili)
                throw new InvalidOperationException("Posti insufficienti per completare la prenotazione");

            var codiceConPosti = GenerateCodicePrenotazione();
            var prezzoTotaleConPosti = dto.NumeroPosti * 10m;

            var prenotazioneConPosti = new Prenotazione
            {
                UserId = userId,
                ProiezioneId = dto.ProiezioneId,
                NumeroPosti = dto.NumeroPosti,
                CreatedAt = DateTime.UtcNow,
                Stato = StatoPrenotazione.InAttesa,
                CodicePrenotazione = codiceConPosti,
                PrezzoTotale = prezzoTotaleConPosti,
                PostiSelezionati = string.Join(',', normalizedRequestedSeats)
            };

            _context.Prenotazioni.Add(prenotazioneConPosti);
            await _context.SaveChangesAsync();

            return MapPrenotazioneDTO(prenotazioneConPosti, proiezione);
        }

        var postiDisponibiliAuto = Math.Max(0, capienzaCinema - postiOccupati.Count);
        if (dto.NumeroPosti > postiDisponibiliAuto)
            throw new InvalidOperationException("Posti insufficienti per completare la prenotazione");

        var postiAssegnatiAuto = tuttiIPosti
            .Where(seat => !postiOccupati.Contains(seat))
            .Take(dto.NumeroPosti)
            .ToArray();

        var codice = GenerateCodicePrenotazione();

        // Calcolo prezzo (esempio: 10€ a posto)
        var prezzoTotale = dto.NumeroPosti * 10m;

        var prenotazione = new Prenotazione
        {
            UserId = userId,
            ProiezioneId = dto.ProiezioneId,
            NumeroPosti = dto.NumeroPosti,
            CreatedAt = DateTime.UtcNow,
            Stato = StatoPrenotazione.InAttesa,
            CodicePrenotazione = codice,
            PrezzoTotale = prezzoTotale,
            PostiSelezionati = string.Join(',', postiAssegnatiAuto)
        };

        _context.Prenotazioni.Add(prenotazione);
        await _context.SaveChangesAsync();

        return MapPrenotazioneDTO(prenotazione, proiezione);
    }

    public async Task<bool> AnnullaAsync(int id, int userId)
    {
        var prenotazione = await _context.Prenotazioni
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (prenotazione == null) return false;

        if (prenotazione.Stato == StatoPrenotazione.Annullata)
            return false;

        prenotazione.Stato = StatoPrenotazione.Annullata;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<PrenotazioneDTO?> GetByCodiceAsync(string codice)
    {
        var prenotazione = await _context.Prenotazioni
            .Where(p => p.CodicePrenotazione == codice)
            .Include(p => p.Proiezione)
            .ThenInclude(p => p.Film)
            .Include(p => p.Proiezione)
            .ThenInclude(p => p.Cinema)
            .FirstOrDefaultAsync();

        if (prenotazione == null) return null;

        return MapPrenotazioneDTO(prenotazione);
    }

    public async Task<PrenotazioneDisponibilitaDTO?> GetDisponibilitaAsync(int proiezioneId)
    {
        var proiezione = await _context.Proiezioni
            .Include(p => p.Film)
            .Include(p => p.Cinema)
            .FirstOrDefaultAsync(p => p.Id == proiezioneId);

        if (proiezione == null) return null;

        var capienzaCinema = Math.Max(1, proiezione.Cinema?.CapienzaTotale ?? 120);

        var prenotazioniAttive = await _context.Prenotazioni
            .Where(p => p.ProiezioneId == proiezioneId && p.Stato != StatoPrenotazione.Annullata)
            .ToListAsync();

        var postiOccupati = prenotazioniAttive
            .SelectMany(GetPrenotazioneSeats)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToArray();

        var tuttiIPosti = GenerateSeatLayout(capienzaCinema);
        var postiDisponibili = Math.Max(0, capienzaCinema - postiOccupati.Length);
        var maxPostiPrenotabili = Math.Min(20, postiDisponibili);

        return new PrenotazioneDisponibilitaDTO(
            proiezione.Id,
            proiezione.CinemaId,
            proiezione.Cinema?.Nome ?? "Cinema",
            proiezione.Cinema?.Citta ?? string.Empty,
            proiezione.FilmId,
            proiezione.Film?.Titolo ?? "Film",
            proiezione.Film?.CopertinaPath,
            proiezione.Data,
            proiezione.Ora.TimeOfDay,
            capienzaCinema,
            postiOccupati.Length,
            postiDisponibili,
            maxPostiPrenotabili,
            postiOccupati,
            tuttiIPosti
        );
    }

    private PrenotazioneDTO MapPrenotazioneDTO(Prenotazione prenotazione, Proiezione? loadedProiezione = null)
    {
        var proiezione = loadedProiezione ?? prenotazione.Proiezione;
        var capienzaCinema = Math.Max(1, proiezione?.Cinema?.CapienzaTotale ?? 120);

        var posti = GetPrenotazioneSeats(prenotazione).ToArray();
        var postiDisponibili = Math.Max(0, capienzaCinema - posti.Length);

        return new PrenotazioneDTO(
            prenotazione.Id,
            prenotazione.CodicePrenotazione ?? "",
            new FilmSummaryDTO(
                proiezione?.Film?.Id ?? 0,
                proiezione?.Film?.Titolo ?? "Film",
                proiezione?.Film?.CopertinaPath
            ),
            new CinemaSummaryDTO(
                proiezione?.Cinema?.Id ?? 0,
                proiezione?.Cinema?.Nome ?? "Cinema",
                proiezione?.Cinema?.Citta ?? string.Empty
            ),
            proiezione?.Data ?? DateTime.MinValue,
            proiezione?.Ora.TimeOfDay ?? TimeSpan.Zero,
            postiDisponibili,
            capienzaCinema,
            prenotazione.NumeroPosti,
            posti,
            prenotazione.PrezzoTotale,
            prenotazione.Stato,
            prenotazione.CreatedAt
        );
    }

    private static IEnumerable<string> GetPrenotazioneSeats(Prenotazione prenotazione)
    {
        if (string.IsNullOrWhiteSpace(prenotazione.PostiSelezionati))
        {
            return Enumerable.Empty<string>();
        }

        return prenotazione.PostiSelezionati
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeSeatCode)
            .Where(s => !string.IsNullOrWhiteSpace(s));
    }

    private static string NormalizeSeatCode(string seatCode)
    {
        return (seatCode ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static string[] GenerateSeatLayout(int capienza)
    {
        var seats = new List<string>(capienza);
        var index = 0;
        while (seats.Count < capienza)
        {
            var rowChar = (char)('A' + (index / 20));
            var seatNumber = (index % 20) + 1;
            seats.Add($"{rowChar}{seatNumber:00}");
            index++;
        }

        return seats.ToArray();
    }

    private static string GenerateCodicePrenotazione()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd");
        var random = new Random();
        var randomPart = random.Next(1000, 9999).ToString();
        return $"PRE-{timestamp}-{randomPart}";
    }
}
