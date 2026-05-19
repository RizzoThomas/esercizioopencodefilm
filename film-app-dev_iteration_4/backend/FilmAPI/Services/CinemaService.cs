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
public class CinemaService : ICinemaService
{
    private readonly FilmDbContext _context;

    /// <summary>
    /// Esegue l''operazione CinemaService del servizio.
    /// </summary>
    /// <param name="context">Parametro necessario per l'operazione: context.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public CinemaService(FilmDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetAllAsync del servizio.
    /// </summary>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<List<CinemaDTO>> GetAllAsync()
    {
        return await _context.Cinemas
            .Select(c => new CinemaDTO
            {
                Id = c.Id,
                Nome = c.Nome,
                Indirizzo = c.Indirizzo,
                Citta = c.Citta
            })
            .ToListAsync();
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetPagedAsync del servizio.
    /// </summary>
    /// <param name="page">Parametro necessario per l'operazione: page.</param>
    /// <param name="pageSize">Parametro necessario per l'operazione: pageSize.</param>
    /// <param name="search">Parametro necessario per l'operazione: search.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<CinemaPagedResultDTO> GetPagedAsync(int page, int pageSize, string? search)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;

        var query = _context.Cinemas.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var likePattern = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Nome, likePattern) ||
                EF.Functions.Like(c.Citta, likePattern) ||
                EF.Functions.Like(c.Indirizzo, likePattern));
        }

        var totalCount = await query.CountAsync();
        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        if (normalizedPage > totalPages)
        {
            normalizedPage = totalPages;
        }

        var items = await query
            .OrderBy(c => c.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(c => new CinemaDTO
            {
                Id = c.Id,
                Nome = c.Nome,
                Indirizzo = c.Indirizzo,
                Citta = c.Citta
            })
            .ToListAsync();

        return new CinemaPagedResultDTO
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
    public async Task<CinemaDTO?> GetByIdAsync(int id)
    {
        var cinema = await _context.Cinemas.FindAsync(id);
        if (cinema is null) return null;

        return new CinemaDTO
        {
            Id = cinema.Id,
            Nome = cinema.Nome,
            Indirizzo = cinema.Indirizzo,
            Citta = cinema.Citta
        };
    }

    /// <summary>
    /// Esegue l''operazione di business CreateAsync del servizio.
    /// </summary>
    /// <param name="dto">Oggetto DTO di input necessario per eseguire l'operazione.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<CinemaDTO> CreateAsync(CinemaCreateDTO dto)
    {
        var cinema = new Cinema
        {
            Nome = dto.Nome,
            Indirizzo = dto.Indirizzo,
            Citta = dto.Citta
        };

        _context.Cinemas.Add(cinema);
        await _context.SaveChangesAsync();

        return new CinemaDTO
        {
            Id = cinema.Id,
            Nome = cinema.Nome,
            Indirizzo = cinema.Indirizzo,
            Citta = cinema.Citta
        };
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
    public async Task<CinemaDTO?> UpdateAsync(int id, CinemaUpdateDTO dto)
    {
        var cinema = await _context.Cinemas.FindAsync(id);
        if (cinema is null) return null;

        cinema.Nome = dto.Nome;
        cinema.Indirizzo = dto.Indirizzo;
        cinema.Citta = dto.Citta;

        await _context.SaveChangesAsync();

        return new CinemaDTO
        {
            Id = cinema.Id,
            Nome = cinema.Nome,
            Indirizzo = cinema.Indirizzo,
            Citta = cinema.Citta
        };
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
        var cinema = await _context.Cinemas.FindAsync(id);
        if (cinema is null) return false;

        _context.Cinemas.Remove(cinema);
        await _context.SaveChangesAsync();
        return true;
    }
}
