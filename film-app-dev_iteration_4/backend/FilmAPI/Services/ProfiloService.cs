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
public class ProfiloService : IProfiloService
{
    private readonly FilmDbContext _context;

    /// <summary>
    /// Esegue l''operazione ProfiloService del servizio.
    /// </summary>
    /// <param name="context">Parametro necessario per l'operazione: context.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public ProfiloService(FilmDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetProfiloAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<UserInfoDTO?> GetProfiloAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return null;

        return MapToUserInfoDTO(user);
    }

    /// <summary>
    /// Esegue l''operazione di business UpdateProfiloAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="dto">Oggetto DTO di input necessario per eseguire l'operazione.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database.
    /// </remarks>
    public async Task<UserInfoDTO?> UpdateProfiloAsync(int userId, ProfiloUpdateDTO dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return null;

        user.Nome = dto.Nome;
        user.Cognome = dto.Cognome;
        user.Telefono = dto.Telefono;

        await _context.SaveChangesAsync();

        return MapToUserInfoDTO(user);
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetCinemaPreferitoAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<CinemaPreferitoDTO?> GetCinemaPreferitoAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.CinemaPreferito)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return null;

        if (user.CinemaPreferito is null)
        {
            return new CinemaPreferitoDTO { CinemaId = null, Cinema = null };
        }

        return new CinemaPreferitoDTO
        {
            CinemaId = user.CinemaPreferitoId,
            Cinema = new CinemaSintesiDTO
            {
                Id = user.CinemaPreferito.Id,
                Nome = user.CinemaPreferito.Nome,
                Citta = user.CinemaPreferito.Citta,
                Indirizzo = user.CinemaPreferito.Indirizzo,
                Telefono = user.CinemaPreferito.Telefono,
                CodiceLocale = user.CinemaPreferito.CodiceLocale
            }
        };
    }

    /// <summary>
    /// Esegue l''operazione SetCinemaPreferitoAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="cinemaId">Identificativo necessario per individuare l'entità o il contesto di lavoro: cinemaId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<CinemaPreferitoDTO> SetCinemaPreferitoAsync(int userId, int? cinemaId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) throw new InvalidOperationException("Utente non trovato");

        if (cinemaId.HasValue)
        {
            var cinemaExists = await _context.Cinemas.AnyAsync(c => c.Id == cinemaId.Value);
            if (!cinemaExists) throw new ArgumentException("Cinema non trovato");
        }

        user.CinemaPreferitoId = cinemaId;
        await _context.SaveChangesAsync();

        return await GetCinemaPreferitoAsync(userId) ?? new CinemaPreferitoDTO { CinemaId = null, Cinema = null };
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetUserSubscriptionAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<UserSubscriptionDTO?> GetUserSubscriptionAsync(int userId)
    {
        var sub = await _context.UserSubscriptions
            .Include(s => s.Abbonamento)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Stato == "attivo");

        if (sub is null) return null;

        return new UserSubscriptionDTO
        {
            Id = sub.Id,
            AbbonamentoId = sub.AbbonamentoId,
            AbbonamentoNome = sub.Abbonamento?.Nome ?? string.Empty,
            AbbonamentoTipo = sub.Abbonamento?.Tipo ?? string.Empty,
            MetodoPagamento = sub.MetodoPagamento,
            AutoRinnovo = sub.AutoRinnovo,
            DataInizio = sub.DataInizio,
            DataScadenza = sub.DataScadenza,
            Stato = sub.Stato,
            NumeroBigliettiPerMese = sub.Abbonamento?.NumeroBigliettiPerMese ?? 0,
            IncludePopcornPerMese = sub.Abbonamento?.IncludePopcornPerMese ?? 0
        };
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetUserVouchersAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<List<UserVoucherDTO>> GetUserVouchersAsync(int userId)
    {
        return await _context.Vouchers
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .Select(v => new UserVoucherDTO
            {
                Id = v.Id,
                Codice = v.Codice,
                Importo = v.SaldoResiduo,
                DataScadenza = v.DataScadenza,
                Stato = v.Stato
            })
            .ToListAsync();
    }

    /// <summary>
    /// Esegue l''operazione di business CancelUserSubscriptionAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<UserSubscriptionDTO?> CancelUserSubscriptionAsync(int userId)
    {
        var sub = await _context.UserSubscriptions
            .Include(s => s.Abbonamento)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Stato == "attivo");

        if (sub is null) return null;

        sub.Stato = "cancellato";
        sub.AutoRinnovo = false;
        await _context.SaveChangesAsync();

        return await GetUserSubscriptionAsync(userId);
    }

    /// <summary>
    /// Esegue l''operazione ToggleAutoRenewAsync del servizio.
    /// </summary>
    /// <param name="userId">Identificativo necessario per individuare l'entità o il contesto di lavoro: userId.</param>
    /// <param name="autoRinnovo">Parametro necessario per l'operazione: autoRinnovo.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può inviare email di notifica.
    /// </remarks>
    public async Task<UserSubscriptionDTO?> ToggleAutoRenewAsync(int userId, bool autoRinnovo)
    {
        var sub = await _context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Stato == "attivo");

        if (sub is null) return null;

        sub.AutoRinnovo = autoRinnovo;
        await _context.SaveChangesAsync();

        return await GetUserSubscriptionAsync(userId);
    }

    private static UserInfoDTO MapToUserInfoDTO(User user)
    {
        return new UserInfoDTO
        {
            Id = user.Id,
            Email = user.Email,
            Nome = user.Nome,
            Cognome = user.Cognome,
            Telefono = user.Telefono,
            Ruolo = user.Ruolo.ToString(),
            DataRegistrazione = user.DataRegistrazione
        };
    }
}
