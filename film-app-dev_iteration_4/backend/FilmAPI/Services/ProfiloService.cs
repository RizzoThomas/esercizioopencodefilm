using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class ProfiloService : IProfiloService
{
    private readonly FilmDbContext _context;

    public ProfiloService(FilmDbContext context)
    {
        _context = context;
    }

    public async Task<UserInfoDTO?> GetProfiloAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return null;

        return MapToUserInfoDTO(user);
    }

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
