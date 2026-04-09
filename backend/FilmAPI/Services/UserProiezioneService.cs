using FilmAPI.Data;
using FilmAPI.DTO.UserProiezione;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public interface IUserProiezioneService
{
    Task<List<UserProiezioneDTO>> GetByUserIdAsync(int userId);
    Task<UserProiezioneDTO?> GetByIdAsync(int id, int userId);
    Task<UserProiezioneDTO> CreateAsync(int userId, UserProiezioneCreateDTO dto);
    Task<bool> DeleteAsync(int id, int userId);
    Task<bool> ExistsAsync(int userId, int proiezioneId);
}

public class UserProiezioneService : IUserProiezioneService
{
    private readonly FilmDbContext _context;

    public UserProiezioneService(FilmDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserProiezioneDTO>> GetByUserIdAsync(int userId)
    {
        return await _context.UserProiezioni
            .Where(up => up.UserId == userId)
            .Include(up => up.Proiezione)
            .ThenInclude(p => p.Film)
            .Include(up => up.Proiezione)
            .ThenInclude(p => p.Cinema)
            .OrderByDescending(up => up.SavedAt)
            .Select(up => new UserProiezioneDTO(
                up.Id,
                up.ProiezioneId,
                new FilmSummaryDTO(
                    up.Proiezione.Film.Id,
                    up.Proiezione.Film.Titolo,
                    up.Proiezione.Film.CopertinaPath
                ),
                new CinemaSummaryDTO(
                    up.Proiezione.Cinema.Id,
                    up.Proiezione.Cinema.Nome,
                    up.Proiezione.Cinema.Citta
                ),
                up.Proiezione.Data,
                up.Proiezione.Ora.TimeOfDay,
                up.SavedAt,
                up.Note
            ))
            .ToListAsync();
    }

    public async Task<UserProiezioneDTO?> GetByIdAsync(int id, int userId)
    {
        var userProiezione = await _context.UserProiezioni
            .Where(up => up.Id == id && up.UserId == userId)
            .Include(up => up.Proiezione)
            .ThenInclude(p => p.Film)
            .Include(up => up.Proiezione)
            .ThenInclude(p => p.Cinema)
            .FirstOrDefaultAsync();

        if (userProiezione == null) return null;

        return new UserProiezioneDTO(
            userProiezione.Id,
            userProiezione.ProiezioneId,
            new FilmSummaryDTO(
                userProiezione.Proiezione.Film.Id,
                userProiezione.Proiezione.Film.Titolo,
                userProiezione.Proiezione.Film.CopertinaPath
            ),
            new CinemaSummaryDTO(
                userProiezione.Proiezione.Cinema.Id,
                userProiezione.Proiezione.Cinema.Nome,
                userProiezione.Proiezione.Cinema.Citta
            ),
            userProiezione.Proiezione.Data,
            userProiezione.Proiezione.Ora.TimeOfDay,
            userProiezione.SavedAt,
            userProiezione.Note
        );
    }

    public async Task<UserProiezioneDTO> CreateAsync(int userId, UserProiezioneCreateDTO dto)
    {
        var proiezione = await _context.Proiezioni
            .Include(p => p.Film)
            .Include(p => p.Cinema)
            .FirstOrDefaultAsync(p => p.Id == dto.ProiezioneId);

        if (proiezione == null)
            throw new ArgumentException("Proiezione non trovata");

        var existing = await _context.UserProiezioni
            .AnyAsync(up => up.UserId == userId && up.ProiezioneId == dto.ProiezioneId);

        if (existing)
            throw new InvalidOperationException("Proiezione già salvata");

        var userProiezione = new UserProiezione
        {
            UserId = userId,
            ProiezioneId = dto.ProiezioneId,
            SavedAt = DateTime.UtcNow,
            Note = dto.Note
        };

        _context.UserProiezioni.Add(userProiezione);
        await _context.SaveChangesAsync();

        return new UserProiezioneDTO(
            userProiezione.Id,
            userProiezione.ProiezioneId,
            new FilmSummaryDTO(
                proiezione.Film.Id,
                proiezione.Film.Titolo,
                proiezione.Film.CopertinaPath
            ),
            new CinemaSummaryDTO(
                proiezione.Cinema.Id,
                proiezione.Cinema.Nome,
                proiezione.Cinema.Citta
            ),
            proiezione.Data,
            proiezione.Ora.TimeOfDay,
            userProiezione.SavedAt,
            userProiezione.Note
        );
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var userProiezione = await _context.UserProiezioni
            .FirstOrDefaultAsync(up => up.Id == id && up.UserId == userId);

        if (userProiezione == null) return false;

        _context.UserProiezioni.Remove(userProiezione);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ExistsAsync(int userId, int proiezioneId)
    {
        return await _context.UserProiezioni
            .AnyAsync(up => up.UserId == userId && up.ProiezioneId == proiezioneId);
    }
}
