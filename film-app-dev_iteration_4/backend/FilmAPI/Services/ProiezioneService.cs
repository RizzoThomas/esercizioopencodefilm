using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

/// <summary>
///     Servizio per la gestione delle proiezioni (versione alternativa/semplificata
///     degli show). Recupera e gestisce le proiezioni con dettagli su film,
///     cinema e sala. Usato principalmente per la visualizzazione pubblica
///     della programmazione.
/// </summary>
public class ProiezioneService : IProiezioneService
{
    /// <summary>DbContext per accesso a show, film, cinema, sale.</summary>
    private readonly FilmDbContext _context;

    /// <summary>Servizio show sottostante per operazioni di base.</summary>
    private readonly IShowService _showService;

    /// <summary>
    ///     Inizializza il servizio con le dipendenze necessarie.
    /// </summary>
    public ProiezioneService(FilmDbContext context, IShowService showService)
    {
        _context = context;
        _showService = showService;
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetAllAsync del servizio.
    /// </summary>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: non introduce effetti collaterali esterni evidenti oltre alla logica di lettura o validazione.
    /// </remarks>
    public async Task<List<ProiezioneDTO>> GetAllAsync()
    {
        return await _context.Shows
            .Include(s => s.Film)
            .Include(s => s.Cinema)
            .Include(s => s.Sala)
            .OrderBy(s => s.StartAtUtc)
            .Select(s => new ProiezioneDTO
            {
                Id = s.Id,
                CinemaId = s.CinemaId,
                FilmId = s.FilmId,
                Data = s.StartAtUtc,
                Ora = s.StartAtUtc
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
    public async Task<ProiezionePagedResultDTO> GetPagedAsync(int page, int pageSize, string? search)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;

        var query = _context.Shows
            .Include(s => s.Cinema)
            .Include(s => s.Film)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var likePattern = $"%{search.Trim()}%";
            query = query.Where(s =>
                EF.Functions.Like(s.Id.ToString(), likePattern) ||
                EF.Functions.Like(s.CinemaId.ToString(), likePattern) ||
                EF.Functions.Like(s.FilmId.ToString(), likePattern) ||
                (s.Cinema != null && EF.Functions.Like(s.Cinema.Nome, likePattern)) ||
                (s.Film != null && EF.Functions.Like(s.Film.Titolo, likePattern)));
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
            .OrderBy(s => s.StartAtUtc)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(s => new ProiezioneDTO
            {
                Id = s.Id,
                CinemaId = s.CinemaId,
                FilmId = s.FilmId,
                Data = s.StartAtUtc,
                Ora = s.StartAtUtc
            })
            .ToListAsync();

        return new ProiezionePagedResultDTO
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
    public async Task<ProiezioneDTO?> GetByIdAsync(int id)
    {
        var show = await _context.Shows.FindAsync(id);
        if (show is null) return null;

        return MapToShowDTO(show);
    }

    /// <summary>
    /// Esegue l''operazione di business CreateAsync del servizio.
    /// </summary>
    /// <param name="dto">Oggetto DTO di input necessario per eseguire l'operazione.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<ProiezioneDTO> CreateAsync(ProiezioneCreateDTO dto)
    {
        var showDto = new ShowCreateDTO
        {
            CinemaId = dto.CinemaId,
            SalaId = await GetDefaultSalaIdAsync(dto.CinemaId),
            FilmId = dto.FilmId,
            StartAtUtc = CombineDateTime(dto.Data, dto.Ora)
        };

        var createdShow = await _showService.CreateAsync(showDto);
        return MapToProiezioneDTO(createdShow);
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
    public async Task<ProiezioneDTO?> UpdateAsync(int id, ProiezioneUpdateDTO dto)
    {
        var show = await _context.Shows.FindAsync(id);
        if (show is null) return null;

        var targetCinemaId = dto.CinemaId ?? show.CinemaId;
        var targetSalaId = dto.CinemaId.HasValue && dto.CinemaId.Value != show.CinemaId
            ? await GetDefaultSalaIdAsync(targetCinemaId)
            : show.SalaId;

        var newData = dto.Data ?? show.StartAtUtc.Date;
        var newOra = dto.Ora ?? show.StartAtUtc;

        var showDto = new ShowUpdateDTO
        {
            CinemaId = targetCinemaId,
            SalaId = targetSalaId,
            FilmId = dto.FilmId,
            StartAtUtc = CombineDateTime(newData, newOra)
        };

        var updatedShow = await _showService.UpdateAsync(id, showDto);
        return updatedShow is null ? null : MapToProiezioneDTO(updatedShow);
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
        return await _showService.DeleteAsync(id);
    }

    private static ProiezioneDTO MapToShowDTO(Show show)
    {
        return new ProiezioneDTO
        {
            Id = show.Id,
            CinemaId = show.CinemaId,
            FilmId = show.FilmId,
            Data = show.StartAtUtc,
            Ora = show.StartAtUtc
        };
    }

    private static ProiezioneDTO MapToProiezioneDTO(ShowDTO showDto)
    {
        return new ProiezioneDTO
        {
            Id = showDto.Id,
            CinemaId = showDto.CinemaId,
            FilmId = showDto.FilmId,
            Data = showDto.StartAtUtc,
            Ora = showDto.StartAtUtc
        };
    }

    private async Task<int> GetDefaultSalaIdAsync(int cinemaId)
    {
        var sala = await _context.Sale
            .Where(s => s.CinemaId == cinemaId && s.IsAttiva)
            .OrderBy(s => s.NumeroProgressivo)
            .FirstOrDefaultAsync();

        if (sala is null)
            throw new ArgumentException(
                $"Nessuna sala attiva trovata per il cinema {cinemaId}. Creare prima una sala.");

        return sala.Id;
    }

    private static DateTime CombineDateTime(DateTime date, DateTime time)
    {
        return new DateTime(
            date.Year, date.Month, date.Day,
            time.Hour, time.Minute, time.Second,
            DateTimeKind.Utc);
    }
}
