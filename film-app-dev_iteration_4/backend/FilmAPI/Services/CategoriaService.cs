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
public class CategoriaService : ICategoriaService
{
    private readonly FilmDbContext _context;

    /// <summary>
    /// Esegue l''operazione CategoriaService del servizio.
    /// </summary>
    /// <param name="context">Parametro necessario per l'operazione: context.</param>
    /// <returns>Restituisce il risultato dell'operazione quando questa ha esito positivo; altrimenti il chiamante riceve un'eccezione o un risultato nullo/booleano secondo il contratto del metodo.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public CategoriaService(FilmDbContext context)
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
    public async Task<List<CategoriaDTO>> GetAllAsync()
    {
        return await _context.Categorie
            .AsNoTracking()
            .Select(c => new CategoriaDTO
            {
                Id = c.Id,
                Nome = c.Nome
            })
            .ToListAsync();
    }

    /// <summary>
    /// Recupera o legge i dati tramite l''operazione GetByIdAsync del servizio.
    /// </summary>
    /// <param name="id">Identificativo necessario per individuare l'entità o il contesto di lavoro: id.</param>
    /// <returns>Restituisce in modo asincrono il risultato dell'operazione indicato dal tipo interno del Task quando la logica termina correttamente.</returns>
    /// <remarks>
    /// Effetti collaterali: scrive o aggiorna il database. può effettuare chiamate a servizi esterni o API HTTP.
    /// </remarks>
    public async Task<CategoriaDTO?> GetByIdAsync(int id)
    {
        var categoria = await _context.Categorie
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (categoria is null) return null;

        return new CategoriaDTO
        {
            Id = categoria.Id,
            Nome = categoria.Nome
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
    public async Task<CategoriaDTO> CreateAsync(CategoriaCreateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new ArgumentException("Il nome della categoria e obbligatorio");
        }

        var normalized = dto.Nome.Trim();
        var exists = await _context.Categorie.AnyAsync(c => c.Nome == normalized);
        if (exists)
        {
            throw new InvalidOperationException($"Categoria '{normalized}' gia esistente");
        }

        var categoria = new Categoria { Nome = normalized };
        _context.Categorie.Add(categoria);
        await _context.SaveChangesAsync();

        return new CategoriaDTO
        {
            Id = categoria.Id,
            Nome = categoria.Nome
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
    public async Task<CategoriaDTO?> UpdateAsync(int id, CategoriaUpdateDTO dto)
    {
        var categoria = await _context.Categorie.FindAsync(id);
        if (categoria is null) return null;

        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new ArgumentException("Il nome della categoria e obbligatorio");
        }

        var normalized = dto.Nome.Trim();
        var exists = await _context.Categorie.AnyAsync(c => c.Nome == normalized && c.Id != id);
        if (exists)
        {
            throw new InvalidOperationException($"Categoria '{normalized}' gia esistente");
        }

        categoria.Nome = normalized;
        await _context.SaveChangesAsync();

        return new CategoriaDTO
        {
            Id = categoria.Id,
            Nome = categoria.Nome
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
        var categoria = await _context.Categorie.FindAsync(id);
        if (categoria is null) return false;

        _context.Categorie.Remove(categoria);
        await _context.SaveChangesAsync();
        return true;
    }
}
