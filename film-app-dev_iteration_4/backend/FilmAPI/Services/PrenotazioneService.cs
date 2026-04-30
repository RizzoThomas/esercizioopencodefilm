using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class PrenotazioneService : IPrenotazioneService
{
    private readonly FilmDbContext _context;

    public PrenotazioneService(FilmDbContext context)
    {
        _context = context;
    }

    public async Task<List<PrenotazioneDTO>> GetPrenotazioniAsync(int userId)
    {
        return await _context.Prenotazioni
            .Where(p => p.UserId == userId)
            .Include(p => p.Proiezione)
                .ThenInclude(pr => pr!.Film)
            .Include(p => p.Proiezione)
                .ThenInclude(pr => pr!.Cinema)
            .Select(p => MapToDTO(p))
            .ToListAsync();
    }

    public async Task<List<PrenotazioneDTO>> GetAllPrenotazioniAsync()
    {
        return await _context.Prenotazioni
            .Include(p => p.Proiezione)
                .ThenInclude(pr => pr!.Film)
            .Include(p => p.Proiezione)
                .ThenInclude(pr => pr!.Cinema)
            .Select(p => MapToDTO(p))
            .ToListAsync();
    }

    public async Task<PrenotazioneDTO?> CreatePrenotazioneAsync(int userId, PrenotazioneCreateDTO dto)
    {
        var proiezione = await _context.Proiezioni
            .Include(p => p.Film)
            .Include(p => p.Cinema)
            .FirstOrDefaultAsync(p => p.Id == dto.ProiezioneId);

        if (proiezione is null)
        {
            throw new InvalidOperationException("Proiezione non trovata");
        }

        var prenotazione = new Prenotazione
        {
            UserId = userId,
            ProiezioneId = dto.ProiezioneId,
            NumeroPosti = dto.NumeroPosti,
            Note = dto.Note,
            DataPrenotazione = DateTime.UtcNow
        };

        _context.Prenotazioni.Add(prenotazione);
        await _context.SaveChangesAsync();

        return new PrenotazioneDTO
        {
            Id = prenotazione.Id,
            ProiezioneId = proiezione.Id,
            TitoloFilm = proiezione.Film?.Titolo ?? string.Empty,
            NomeCinema = proiezione.Cinema?.Nome ?? string.Empty,
            DataProiezione = proiezione.Data,
            OraProiezione = proiezione.Ora,
            NumeroPosti = prenotazione.NumeroPosti,
            Note = prenotazione.Note,
            DataPrenotazione = prenotazione.DataPrenotazione
        };
    }

    public async Task<bool> DeletePrenotazioneAsync(int userId, int prenotazioneId)
    {
        var prenotazione = await _context.Prenotazioni
            .FirstOrDefaultAsync(p => p.Id == prenotazioneId && p.UserId == userId);

        if (prenotazione is null) return false;

        _context.Prenotazioni.Remove(prenotazione);
        await _context.SaveChangesAsync();
        return true;
    }

    private static PrenotazioneDTO MapToDTO(Prenotazione prenotazione)
    {
        return new PrenotazioneDTO
        {
            Id = prenotazione.Id,
            ProiezioneId = prenotazione.ProiezioneId,
            TitoloFilm = prenotazione.Proiezione?.Film?.Titolo ?? string.Empty,
            NomeCinema = prenotazione.Proiezione?.Cinema?.Nome ?? string.Empty,
            DataProiezione = prenotazione.Proiezione?.Data ?? DateTime.MinValue,
            OraProiezione = prenotazione.Proiezione?.Ora ?? DateTime.MinValue,
            NumeroPosti = prenotazione.NumeroPosti,
            Note = prenotazione.Note,
            DataPrenotazione = prenotazione.DataPrenotazione
        };
    }
}
