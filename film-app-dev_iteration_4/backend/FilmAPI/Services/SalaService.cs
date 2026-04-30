using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class SalaService : ISalaService
{
    private readonly FilmDbContext _context;

    public SalaService(FilmDbContext context)
    {
        _context = context;
    }

    public async Task<List<SalaDTO>> GetByCinemaAsync(int cinemaId)
    {
        var sale = await _context.Sale
            .Include(s => s.Posti)
            .Where(s => s.CinemaId == cinemaId)
            .OrderBy(s => s.NumeroProgressivo)
            .ToListAsync();

        return sale.Select(MapToDTO).ToList();
    }

    public async Task<SalaDTO?> GetByIdAsync(int id)
    {
        var sala = await _context.Sale
            .Include(s => s.Posti)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sala is null) return null;

        return MapToDTO(sala);
    }

    public async Task<SalaDTO> CreateAsync(SalaCreateDTO dto)
    {
        var cinemaExists = await _context.Cinemas.AnyAsync(c => c.Id == dto.CinemaId);
        if (!cinemaExists)
            throw new ArgumentException($"Cinema con ID {dto.CinemaId} non trovato.");

        var numeroExists = await _context.Sale
            .AnyAsync(s => s.CinemaId == dto.CinemaId && s.NumeroProgressivo == dto.NumeroProgressivo);
        if (numeroExists)
            throw new InvalidOperationException(
                $"Sala numero {dto.NumeroProgressivo} gia esistente per questo cinema.");

        var nome = string.IsNullOrWhiteSpace(dto.Nome)
            ? $"Sala {dto.NumeroProgressivo}"
            : dto.Nome.Trim();

        var sala = new Sala
        {
            CinemaId = dto.CinemaId,
            NumeroProgressivo = dto.NumeroProgressivo,
            TipoSala = dto.TipoSala,
            Nome = nome,
            Supplemento = dto.Supplemento,
            IsAttiva = dto.IsAttiva
        };

        _context.Sale.Add(sala);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(sala.Id) ?? throw new InvalidOperationException("Errore imprevisto dopo la creazione della sala.");
    }

    public async Task<SalaDTO?> UpdateAsync(int id, SalaUpdateDTO dto)
    {
        var sala = await _context.Sale.FindAsync(id);
        if (sala is null) return null;

        sala.TipoSala = dto.TipoSala;
        sala.Supplemento = dto.Supplemento;
        sala.IsAttiva = dto.IsAttiva;

        if (!string.IsNullOrWhiteSpace(dto.Nome))
        {
            sala.Nome = dto.Nome.Trim();
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(sala.Id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sala = await _context.Sale
            .Include(s => s.Posti)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (sala is null) return false;

        var now = DateTime.UtcNow;
        var hasFutureShows = await _context.Shows
            .AnyAsync(sh => sh.SalaId == id && sh.StartAtUtc > now);
        if (hasFutureShows)
            throw new InvalidOperationException(
                "Impossibile eliminare la sala: esistono show futuri programmati.");

        var hasIssuedTickets = await _context.Biglietti
            .AnyAsync(b => b.SalaPostoId > 0 &&
                           _context.SalaPosti.Any(sp => sp.Id == b.SalaPostoId && sp.SalaId == id));
        if (hasIssuedTickets)
            throw new InvalidOperationException(
                "Impossibile eliminare la sala: esistono biglietti emessi per questa sala.");

        _context.Sale.Remove(sala);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<SalaPostoDTO>> GetPostiAsync(int salaId)
    {
        var salaExists = await _context.Sale.AnyAsync(s => s.Id == salaId);
        if (!salaExists)
            throw new ArgumentException($"Sala con ID {salaId} non trovata.");

        var posti = await _context.SalaPosti
            .Where(p => p.SalaId == salaId)
            .OrderBy(p => p.Settore)
            .ThenBy(p => p.Fila)
            .ThenBy(p => p.Numero)
            .ToListAsync();

        return posti.Select(MapPostoToDTO).ToList();
    }

    public async Task<List<SalaPostoDTO>> SavePostiAsync(int salaId, SalaLayoutSaveDTO dto)
    {
        var sala = await _context.Sale.FindAsync(salaId);
        if (sala is null)
            throw new ArgumentException($"Sala con ID {salaId} non trovata.");

        var existingPosti = await _context.SalaPosti
            .Where(p => p.SalaId == salaId)
            .ToListAsync();

        _context.SalaPosti.RemoveRange(existingPosti);

        foreach (var postoDto in dto.Posti)
        {
            var posto = new SalaPosto
            {
                SalaId = salaId,
                Settore = string.IsNullOrWhiteSpace(postoDto.Settore) ? "PLATEA" : postoDto.Settore.Trim(),
                Fila = postoDto.Fila,
                Numero = postoDto.Numero,
                PosX = postoDto.PosX,
                PosY = postoDto.PosY,
                IsWheelchair = postoDto.IsWheelchair,
                IsAttivo = postoDto.IsAttivo
            };

            _context.SalaPosti.Add(posto);
        }

        await _context.SaveChangesAsync();

        return await GetPostiAsync(salaId);
    }

    private static SalaDTO MapToDTO(Sala sala)
    {
        return new SalaDTO
        {
            Id = sala.Id,
            CinemaId = sala.CinemaId,
            NumeroProgressivo = sala.NumeroProgressivo,
            TipoSala = sala.TipoSala,
            Nome = sala.Nome,
            Supplemento = sala.Supplemento,
            IsAttiva = sala.IsAttiva,
            Posti = sala.Posti.Select(MapPostoToDTO).ToList()
        };
    }

    private static SalaPostoDTO MapPostoToDTO(SalaPosto posto)
    {
        return new SalaPostoDTO
        {
            Id = posto.Id,
            SalaId = posto.SalaId,
            Settore = posto.Settore,
            Fila = posto.Fila,
            Numero = posto.Numero,
            PosX = posto.PosX,
            PosY = posto.PosY,
            IsWheelchair = posto.IsWheelchair,
            IsAttivo = posto.IsAttivo
        };
    }
}
