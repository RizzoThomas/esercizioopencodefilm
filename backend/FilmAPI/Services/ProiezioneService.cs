using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public interface IProiezioneService
{
    Task<List<ProiezioneDTO>> GetAllAsync();
    Task<ProiezioneDTO?> GetByIdAsync(int id);
    Task<ProiezioneDTO> CreateAsync(ProiezioneCreateDTO dto);
    Task<ProiezioneDTO?> UpdateAsync(int id, ProiezioneUpdateDTO dto);
    Task<bool> DeleteAsync(int id);
}

public class ProiezioneService : IProiezioneService
{
    private readonly FilmDbContext _context;

    public ProiezioneService(FilmDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProiezioneDTO>> GetAllAsync()
    {
        return await _context.Proiezioni
            .Select(p => new ProiezioneDTO
            {
                Id = p.Id,
                CinemaId = p.CinemaId,
                FilmId = p.FilmId,
                Data = p.Data,
                Ora = p.Ora.ToString("HH:mm:ss")
            })
            .ToListAsync();
    }

    public async Task<ProiezioneDTO?> GetByIdAsync(int id)
    {
        var proiezione = await _context.Proiezioni.FindAsync(id);
        if (proiezione is null) return null;

        return new ProiezioneDTO
        {
            Id = proiezione.Id,
            CinemaId = proiezione.CinemaId,
            FilmId = proiezione.FilmId,
            Data = proiezione.Data,
            Ora = proiezione.Ora.ToString("HH:mm:ss")
        };
    }

    public async Task<ProiezioneDTO> CreateAsync(ProiezioneCreateDTO dto)
    {
        var film = await _context.Films.FindAsync(dto.FilmId);
        if (film is null)
        {
            throw new ArgumentException("Film non trovato");
        }

        var cinema = await _context.Cinemas.FindAsync(dto.CinemaId);
        if (cinema is null)
        {
            throw new ArgumentException("Cinema non trovato");
        }

        var oraParsed = TimeOnly.ParseExact(dto.Ora, "HH:mm");
        var oraDateTime = new DateTime(1, 1, 1, oraParsed.Hour, oraParsed.Minute, 0);

        var proiezione = new Proiezione
        {
            CinemaId = dto.CinemaId,
            FilmId = dto.FilmId,
            Data = dto.Data,
            Ora = oraDateTime
        };

        var existing = await _context.Proiezioni
            .AnyAsync(p => p.CinemaId == dto.CinemaId 
                && p.FilmId == dto.FilmId 
                && p.Data == dto.Data 
                && p.Ora == oraDateTime);
        
        if (existing)
        {
            throw new InvalidOperationException("Esiste già una proiezione per questo cinema, film, data e ora");
        }

        _context.Proiezioni.Add(proiezione);
        await _context.SaveChangesAsync();

        return new ProiezioneDTO
        {
            Id = proiezione.Id,
            CinemaId = proiezione.CinemaId,
            FilmId = proiezione.FilmId,
            Data = proiezione.Data,
            Ora = proiezione.Ora.ToString("HH:mm:ss")
        };
    }

    public async Task<ProiezioneDTO?> UpdateAsync(int id, ProiezioneUpdateDTO dto)
    {
        var proiezione = await _context.Proiezioni.FindAsync(id);
        if (proiezione is null) return null;

        var film = await _context.Films.FindAsync(dto.FilmId);
        if (film is null)
        {
            throw new ArgumentException("Film non trovato");
        }

        var cinema = await _context.Cinemas.FindAsync(dto.CinemaId);
        if (cinema is null)
        {
            throw new ArgumentException("Cinema non trovato");
        }

        proiezione.CinemaId = dto.CinemaId;
        proiezione.FilmId = dto.FilmId;
        proiezione.Data = dto.Data;
        var oraUpdated = TimeOnly.ParseExact(dto.Ora, "HH:mm");
        proiezione.Ora = new DateTime(1, 1, 1, oraUpdated.Hour, oraUpdated.Minute, 0);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Esiste già una proiezione per questo cinema, film, data e ora");
        }

        return new ProiezioneDTO
        {
            Id = proiezione.Id,
            CinemaId = proiezione.CinemaId,
            FilmId = proiezione.FilmId,
            Data = proiezione.Data,
            Ora = proiezione.Ora.ToString("HH:mm:ss")
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var proiezione = await _context.Proiezioni.FindAsync(id);
        if (proiezione is null) return false;

        _context.Proiezioni.Remove(proiezione);
        await _context.SaveChangesAsync();
        return true;
    }
}
