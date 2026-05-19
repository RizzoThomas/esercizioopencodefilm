using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

/// <summary>
/// Fornisce il servizio  per le operazioni di dominio esposte da questo modulo.
/// </summary>
/// <remarks>
/// Usato dai controller o endpoint che gestiscono le funzioni di . Dipendenze iniettate nel costruttore: nessuna dichiarata esplicitamente.
/// </remarks>
public class ShowService : IShowService
{
    private readonly FilmDbContext _context;

    /// <summary>
    /// Esegue l''operazione ShowService del servizio.
    /// </summary>
    /// <param name="context">Parametro necessario per l'operazione: context.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public ShowService(FilmDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetAllAsync del servizio.
    /// </summary>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<List<ShowDTO>> GetAllAsync()
    {
        return await _context.Shows
            .Include(s => s.Film)
            .Include(s => s.Cinema)
            .Include(s => s.Sala)
            .OrderBy(s => s.StartAtUtc)
            .Select(s => MapToDTO(s))
            .ToListAsync();
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetPagedAsync del servizio.
    /// </summary>
    /// <param name="page">Parametro necessario per l'operazione: page.</param>
    /// <param name="pageSize">Parametro necessario per l'operazione: pageSize.</param>
    /// <param name="cinemaId">Identificativo necessario per individuare l'entità o il contesto di lavoro: cinemaId.</param>
    /// <param name="filmId">Identificativo necessario per individuare l'entità o il contesto di lavoro: filmId.</param>
    /// <param name="date">Parametro necessario per l'operazione: date.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<ShowPagedResultDTO> GetPagedAsync(int page, int pageSize, int? cinemaId = null, int? filmId = null, DateTime? date = null)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;

        var query = _context.Shows
            .Include(s => s.Film)
            .Include(s => s.Cinema)
            .Include(s => s.Sala)
            .AsNoTracking()
            .AsQueryable();

        if (cinemaId.HasValue)
        {
            query = query.Where(s => s.CinemaId == cinemaId.Value);
        }

        if (filmId.HasValue)
        {
            query = query.Where(s => s.FilmId == filmId.Value);
        }

        if (date.HasValue)
        {
            var dayStart = date.Value.Date;
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(s => s.StartAtUtc >= dayStart && s.StartAtUtc < dayEnd);
        }

        var totalCount = await query.CountAsync();
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        if (normalizedPage > totalPages)
        {
            normalizedPage = totalPages;
        }

        var items = await query
            .OrderBy(s => s.StartAtUtc)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(s => MapToDTO(s))
            .ToListAsync();

        return new ShowPagedResultDTO
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetByIdAsync del servizio.
    /// </summary>
    /// <param name="id">Identificativo necessario per individuare l'entità o il contesto di lavoro: id.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<ShowDTO?> GetByIdAsync(int id)
    {
        var show = await _context.Shows
            .Include(s => s.Film)
            .Include(s => s.Cinema)
            .Include(s => s.Sala)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (show is null) return null;

        return MapToDTO(show);
    }

    /// <summary>
    /// Esegue l''operazione di business CreateAsync del servizio.
    /// </summary>
    /// <param name="dto">Oggetto DTO di input necessario per eseguire l'operazione.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<ShowDTO> CreateAsync(ShowCreateDTO dto)
    {
        var film = await _context.Films.FindAsync(dto.FilmId);
        if (film is null)
            throw new ArgumentException("Film non trovato.");

        var cinema = await _context.Cinemas.FindAsync(dto.CinemaId);
        if (cinema is null)
            throw new ArgumentException("Cinema non trovato.");

        var sala = await _context.Sale.FindAsync(dto.SalaId);
        if (sala is null)
            throw new ArgumentException("Sala non trovata.");

        if (sala.CinemaId != dto.CinemaId)
            throw new ArgumentException("La sala non appartiene al cinema specificato.");

        var durata = dto.DurataMinutiSnapshot ?? film.Durata;
        if (durata <= 0)
            throw new ArgumentException("Durata non valida. Specificare DurataMinutiSnapshot o assicurarsi che il film abbia una durata configurata.");

        var prezzoBase = TicketPriceNormalizer.NormalizeUnitPrice(dto.PrezzoBase ?? 10m);

        var startAtUtc = dto.StartAtUtc;
        var endAtUtc = startAtUtc.AddMinutes(durata);

        await ValidateNoOverlapAsync(dto.SalaId, startAtUtc, endAtUtc, excludeShowId: null);

        var show = new Show
        {
            CinemaId = dto.CinemaId,
            SalaId = dto.SalaId,
            FilmId = dto.FilmId,
            StartAtUtc = startAtUtc,
            DurataMinutiSnapshot = durata,
            PrezzoBase = prezzoBase,
            SupplementoSala = TicketPriceNormalizer.NormalizeUnitPrice(sala.Supplemento)
        };

        _context.Shows.Add(show);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(show.Id)
            ?? throw new InvalidOperationException("Errore imprevisto dopo la creazione dello show.");
    }

    /// <summary>
    /// Esegue l''operazione di business UpdateAsync del servizio.
    /// </summary>
    /// <param name="id">Identificativo necessario per individuare l'entità o il contesto di lavoro: id.</param>
    /// <param name="dto">Oggetto DTO di input necessario per eseguire l'operazione.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<ShowDTO?> UpdateAsync(int id, ShowUpdateDTO dto)
    {
        var show = await _context.Shows
            .Include(s => s.Film)
            .Include(s => s.Sala)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (show is null) return null;

        var newFilmId = dto.FilmId ?? show.FilmId;
        var newSalaId = dto.SalaId ?? show.SalaId;
        var newCinemaId = dto.CinemaId ?? show.CinemaId;
        var newStartAtUtc = dto.StartAtUtc ?? show.StartAtUtc;
        var newDurata = dto.DurataMinutiSnapshot ?? show.DurataMinutiSnapshot;
        var newPrezzoBase = TicketPriceNormalizer.NormalizeUnitPrice(dto.PrezzoBase ?? show.PrezzoBase);

        var film = await _context.Films.FindAsync(newFilmId);
        if (film is null)
            throw new ArgumentException("Film non trovato.");

        var sala = await _context.Sale.FindAsync(newSalaId);
        if (sala is null)
            throw new ArgumentException("Sala non trovata.");

        var cinema = await _context.Cinemas.FindAsync(newCinemaId);
        if (cinema is null)
            throw new ArgumentException("Cinema non trovato.");

        if (sala.CinemaId != newCinemaId)
            throw new ArgumentException("La sala non appartiene al cinema specificato.");

        var endAtUtc = newStartAtUtc.AddMinutes(newDurata);

        await ValidateNoOverlapAsync(newSalaId, newStartAtUtc, endAtUtc, excludeShowId: id);

        show.CinemaId = newCinemaId;
        show.SalaId = newSalaId;
        show.FilmId = newFilmId;
        show.StartAtUtc = newStartAtUtc;
        show.DurataMinutiSnapshot = newDurata;
        show.PrezzoBase = newPrezzoBase;
        show.SupplementoSala = TicketPriceNormalizer.NormalizeUnitPrice(sala.Supplemento);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(show.Id);
    }

    /// <summary>
    /// Esegue l''operazione di business DeleteAsync del servizio.
    /// </summary>
    /// <param name="id">Identificativo necessario per individuare l'entità o il contesto di lavoro: id.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<bool> DeleteAsync(int id)
    {
        var show = await _context.Shows.FindAsync(id);
        if (show is null) return false;

        var hasSoldTickets = await _context.Biglietti
            .AnyAsync(b => b.ShowId == id && b.Stato != BigliettoState.Cancelled);

        if (hasSoldTickets)
            throw new InvalidOperationException(
                "Impossibile eliminare lo show: esistono biglietti emessi.");

        _context.Shows.Remove(show);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetByCinemaAsync del servizio.
    /// </summary>
    /// <param name="cinemaId">Identificativo necessario per individuare l'entità o il contesto di lavoro: cinemaId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public async Task<List<ShowDTO>> GetByCinemaAsync(int cinemaId)
    {
        return await _context.Shows
            .Include(s => s.Film)
            .Include(s => s.Cinema)
            .Include(s => s.Sala)
            .Where(s => s.CinemaId == cinemaId)
            .OrderBy(s => s.StartAtUtc)
            .Select(s => MapToDTO(s))
            .ToListAsync();
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetByFilmAsync del servizio.
    /// </summary>
    /// <param name="filmId">Identificativo necessario per individuare l'entità o il contesto di lavoro: filmId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public async Task<List<ShowDTO>> GetByFilmAsync(int filmId)
    {
        return await _context.Shows
            .Include(s => s.Film)
            .Include(s => s.Cinema)
            .Include(s => s.Sala)
            .Where(s => s.FilmId == filmId)
            .OrderBy(s => s.StartAtUtc)
            .Select(s => MapToDTO(s))
            .ToListAsync();
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetByDateAsync del servizio.
    /// </summary>
    /// <param name="date">Parametro necessario per l'operazione: date.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public async Task<List<ShowDTO>> GetByDateAsync(DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        return await _context.Shows
            .Include(s => s.Film)
            .Include(s => s.Cinema)
            .Include(s => s.Sala)
            .Where(s => s.StartAtUtc >= dayStart && s.StartAtUtc < dayEnd)
            .OrderBy(s => s.StartAtUtc)
            .Select(s => MapToDTO(s))
            .ToListAsync();
    }

    private async Task ValidateNoOverlapAsync(int salaId, DateTime newStartAtUtc, DateTime newEndAtUtc, int? excludeShowId)
    {
        var query = _context.Shows
            .Where(s => s.SalaId == salaId
                && s.StartAtUtc < newEndAtUtc
                && s.StartAtUtc.AddMinutes(s.DurataMinutiSnapshot) > newStartAtUtc);

        if (excludeShowId.HasValue)
        {
            query = query.Where(s => s.Id != excludeShowId.Value);
        }

        var overlappingShow = await query.FirstOrDefaultAsync();

        if (overlappingShow is not null)
        {
            var existingEnd = overlappingShow.StartAtUtc.AddMinutes(overlappingShow.DurataMinutiSnapshot);
            throw new InvalidOperationException(
                $"Sovrapposizione con show esistente (ID: {overlappingShow.Id}) " +
                $"nella stessa sala: {overlappingShow.StartAtUtc:HH:mm} - {existingEnd:HH:mm}.");
        }
    }

    private static ShowDTO MapToDTO(Show show)
    {
        return new ShowDTO
        {
            Id = show.Id,
            CinemaId = show.CinemaId,
            SalaId = show.SalaId,
            FilmId = show.FilmId,
            StartAtUtc = show.StartAtUtc,
            DurataMinutiSnapshot = show.DurataMinutiSnapshot,
            PrezzoBase = TicketPriceNormalizer.NormalizeUnitPrice(show.PrezzoBase),
            SupplementoSala = TicketPriceNormalizer.NormalizeUnitPrice(show.SupplementoSala),
            FilmTitolo = show.Film?.Titolo,
            CinemaNome = show.Cinema?.Nome,
            SalaNome = show.Sala?.Nome,
            SalaTipo = show.Sala?.TipoSala
        };
    }
}
