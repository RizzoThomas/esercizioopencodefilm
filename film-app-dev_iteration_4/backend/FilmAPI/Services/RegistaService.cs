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
public class RegistaService : IRegistaService
{
    private readonly FilmDbContext _context;
    private readonly string _defaultCoverPath;

    /// <summary>
    /// Esegue l''operazione RegistaService del servizio.
    /// </summary>
    /// <param name="context">Parametro necessario per l'operazione: context.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public RegistaService(FilmDbContext context)
    {
        _context = context;
        _defaultCoverPath = Environment.GetEnvironmentVariable("DEFAULT_COVER_IMAGE_PATH") ?? "/media/defaults/cover-default.jpg";
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetAllAsync del servizio.
    /// </summary>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<List<RegistaDTO>> GetAllAsync()
    {
        return await _context.Registi
            .Select(r => new RegistaDTO
            {
                Id = r.Id,
                Nome = r.Nome,
                Cognome = r.Cognome,
                Nazionalita = r.Nazionalita
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
    public async Task<RegistaPagedResultDTO> GetPagedAsync(int page, int pageSize, string? search)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;

        var query = _context.Registi.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var likePattern = $"%{search.Trim()}%";
            query = query.Where(r =>
                EF.Functions.Like(r.Nome, likePattern) ||
                EF.Functions.Like(r.Cognome, likePattern) ||
                EF.Functions.Like(r.Nazionalita, likePattern));
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
            .OrderBy(r => r.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(r => new RegistaDTO
            {
                Id = r.Id,
                Nome = r.Nome,
                Cognome = r.Cognome,
                Nazionalita = r.Nazionalita
            })
            .ToListAsync();

        return new RegistaPagedResultDTO
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
    public async Task<RegistaDTO?> GetByIdAsync(int id)
    {
        var regista = await _context.Registi.FindAsync(id);
        if (regista is null) return null;

        return new RegistaDTO
        {
            Id = regista.Id,
            Nome = regista.Nome,
            Cognome = regista.Cognome,
            Nazionalita = regista.Nazionalita
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
    public async Task<RegistaDTO> CreateAsync(RegistaCreateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome) ||
            string.IsNullOrWhiteSpace(dto.Cognome) ||
            string.IsNullOrWhiteSpace(dto.Nazionalita))
        {
            throw new ArgumentException("Nome, Cognome e Nazionalità sono obbligatori");
        }

        var regista = new Regista
        {
            Nome = dto.Nome,
            Cognome = dto.Cognome,
            Nazionalita = dto.Nazionalita
        };

        _context.Registi.Add(regista);
        await _context.SaveChangesAsync();

        return new RegistaDTO
        {
            Id = regista.Id,
            Nome = regista.Nome,
            Cognome = regista.Cognome,
            Nazionalita = regista.Nazionalita
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
    public async Task<RegistaDTO?> UpdateAsync(int id, RegistaUpdateDTO dto)
    {
        var regista = await _context.Registi.FindAsync(id);
        if (regista is null) return null;

        if (string.IsNullOrWhiteSpace(dto.Nome) ||
            string.IsNullOrWhiteSpace(dto.Cognome) ||
            string.IsNullOrWhiteSpace(dto.Nazionalita))
        {
            throw new ArgumentException("Nome, Cognome e Nazionalità sono obbligatori");
        }

        regista.Nome = dto.Nome;
        regista.Cognome = dto.Cognome;
        regista.Nazionalita = dto.Nazionalita;

        await _context.SaveChangesAsync();

        return new RegistaDTO
        {
            Id = regista.Id,
            Nome = regista.Nome,
            Cognome = regista.Cognome,
            Nazionalita = regista.Nazionalita
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
        var regista = await _context.Registi.FindAsync(id);
        if (regista is null) return false;

        _context.Registi.Remove(regista);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetFilmsByRegistaIdAsync del servizio.
    /// </summary>
    /// <param name="id">Identificativo necessario per individuare l'entità o il contesto di lavoro: id.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public async Task<List<FilmDTO>> GetFilmsByRegistaIdAsync(int id)
    {
        var regista = await _context.Registi
            .Include(r => r.Films)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (regista is null) return new List<FilmDTO>();

        return regista.Films.Select(f => new FilmDTO
        {
            Id = f.Id,
            Titolo = f.Titolo,
            DataProduzione = f.DataProduzione,
            RegistaId = f.RegistaId,
            RegistaNome = regista.Nome,
            RegistaCognome = regista.Cognome,
            Durata = f.Durata,
            CopertinaPath = f.CopertinaPath ?? _defaultCoverPath,
            FilmatoPath = f.FilmatoPath
        }).ToList();
    }
}
