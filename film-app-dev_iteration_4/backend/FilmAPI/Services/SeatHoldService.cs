using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class SeatHoldService : ISeatHoldService
{
    private readonly FilmDbContext _db;
    private readonly TimeSpan _holdTtl;
    private const int MaxSeatsPerOrder = 10;

    public SeatHoldService(FilmDbContext db, IConfiguration? configuration = null)
    {
        _db = db;
        var holdTtlMinutes = configuration != null
            ? int.Parse(configuration["HOLD_TTL_MINUTES"] ?? "10")
            : 10;
        _holdTtl = TimeSpan.FromMinutes(holdTtlMinutes);
    }

    public async Task<SeatMapDTO> GetSeatMapAsync(int showId, int userId)
    {
        var now = DateTime.UtcNow;

        await CleanupExpiredHoldsForShowAsync(showId);

        var show = await _db.Shows
            .Include(s => s.Sala)
            .Include(s => s.Film)
            .Include(s => s.Cinema)
            .FirstOrDefaultAsync(s => s.Id == showId);

        if (show == null)
            throw new InvalidOperationException("Show non trovato.");

        var posti = await _db.SalaPosti
            .Where(p => p.SalaId == show.SalaId && p.IsAttivo)
            .ToListAsync();

        var stati = await _db.ShowPostiStato
            .Where(sps => sps.ShowId == showId)
            .ToListAsync();

        var statiDict = stati.ToDictionary(sps => sps.SalaPostoId);

        var seatInfos = new List<SeatInfoDTO>();
        DateTime? myScadeAtUtc = null;

        foreach (var posto in posti)
        {
            var status = SeatStatus.Available;

            if (statiDict.TryGetValue(posto.Id, out var stato))
            {
                if (stato.Stato == ShowPostoState.Sold)
                {
                    status = SeatStatus.Sold;
                }
                else if (stato.Stato == ShowPostoState.Hold)
                {
                    if (stato.ScadeAtUtc <= now)
                    {
                        status = SeatStatus.Available;
                    }
                    else if (stato.UserId == userId)
                    {
                        status = SeatStatus.HeldByMe;
                        if (stato.ScadeAtUtc.HasValue)
                        {
                            myScadeAtUtc = stato.ScadeAtUtc.Value;
                        }
                    }
                    else
                    {
                        status = SeatStatus.HeldByOther;
                    }
                }
            }

            seatInfos.Add(new SeatInfoDTO
            {
                SalaPostoId = posto.Id,
                Settore = posto.Settore,
                Fila = posto.Fila,
                Numero = posto.Numero,
                IsWheelchair = posto.IsWheelchair,
                Stato = status
            });
        }

        return new SeatMapDTO
        {
            ShowId = showId,
            FilmTitolo = show.Film!.Titolo,
            CinemaNome = show.Cinema!.Nome,
            SalaNome = show.Sala!.Nome ?? $"Sala {show.Sala.NumeroProgressivo}",
            StartAtUtc = show.StartAtUtc,
            PrezzoBase = TicketPriceNormalizer.NormalizeUnitPrice(show.PrezzoBase),
            SupplementoSala = TicketPriceNormalizer.NormalizeUnitPrice(show.SupplementoSala),
            ScadeAtUtc = myScadeAtUtc,
            Posti = seatInfos
        };
    }

    public async Task<SeatHoldResponseDTO> CreateHoldAsync(int showId, int userId, List<int> salaPostoIds)
    {
        if (salaPostoIds.Count == 0)
            throw new ArgumentException("Nessun posto selezionato.");

        if (salaPostoIds.Count > MaxSeatsPerOrder)
            throw new ArgumentException($"Massimo {MaxSeatsPerOrder} posti per ordine.");

        var now = DateTime.UtcNow;
        var expiresAt = now.Add(_holdTtl);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        await CleanupExpiredHoldsForShowAsync(showId);

        var show = await _db.Shows
            .Include(s => s.Sala)
            .FirstOrDefaultAsync(s => s.Id == showId);

        if (show == null)
            throw new InvalidOperationException("Show non trovato.");

        var postiValidi = await _db.SalaPosti
            .Where(p => salaPostoIds.Contains(p.Id) && p.SalaId == show.SalaId && p.IsAttivo)
            .ToListAsync();

        if (postiValidi.Count != salaPostoIds.Count)
            throw new ArgumentException("Uno o piu posti non appartengono alla sala dello show o non sono attivi.");

        var statiEsistenti = await _db.ShowPostiStato
            .Where(sps => sps.ShowId == showId && salaPostoIds.Contains(sps.SalaPostoId))
            .ToListAsync();

        var conflitti = new List<string>();
        var postiDaAcquisire = new List<int>();

        foreach (var postoId in salaPostoIds)
        {
            var esistente = statiEsistenti.FirstOrDefault(sps => sps.SalaPostoId == postoId);
            if (esistente != null)
            {
                if (esistente.Stato == ShowPostoState.Sold)
                {
                    conflitti.Add($"Posto {postoId} gia venduto.");
                }
                else if (esistente.Stato == ShowPostoState.Hold)
                {
                    if (esistente.ScadeAtUtc > now)
                    {
                        if (esistente.UserId == userId)
                        {
                            postiDaAcquisire.Add(postoId);
                        }
                        else
                        {
                            conflitti.Add($"Posto {postoId} gia prenotato da altro utente.");
                        }
                    }
                    else
                    {
                        postiDaAcquisire.Add(postoId);
                    }
                }
            }
            else
            {
                postiDaAcquisire.Add(postoId);
            }
        }

        if (conflitti.Count > 0)
        {
            await transaction.RollbackAsync();
            return new SeatHoldResponseDTO
            {
                HoldToken = string.Empty,
                ScadeAtUtc = expiresAt,
                SalaPostoIds = new List<int>(),
                Conflitti = conflitti
            };
        }

        var holdToken = $"{userId}_{showId}_{Guid.NewGuid():N}";

        foreach (var postoId in postiDaAcquisire)
        {
            var stato = statiEsistenti.FirstOrDefault(sps => sps.SalaPostoId == postoId);
            if (stato == null)
            {
                stato = new ShowPostoStato
                {
                    ShowId = showId,
                    SalaPostoId = postoId,
                    UserId = userId,
                    Stato = ShowPostoState.Hold,
                    HoldToken = holdToken,
                    ScadeAtUtc = expiresAt,
                    UpdatedAtUtc = now
                };
                _db.ShowPostiStato.Add(stato);
            }
            else
            {
                stato.UserId = userId;
                stato.Stato = ShowPostoState.Hold;
                stato.HoldToken = holdToken;
                stato.ScadeAtUtc = expiresAt;
                stato.UpdatedAtUtc = now;
            }
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new SeatHoldResponseDTO
        {
            HoldToken = holdToken,
            ScadeAtUtc = expiresAt,
            SalaPostoIds = postiDaAcquisire,
            Conflitti = new List<string>()
        };
    }

    public async Task<SeatHoldResponseDTO> RefreshHoldAsync(string holdToken, int userId)
    {
        var now = DateTime.UtcNow;
        var newExpiresAt = now.Add(_holdTtl);

        var stati = await _db.ShowPostiStato
            .Where(sps => sps.HoldToken == holdToken && sps.UserId == userId && sps.Stato == ShowPostoState.Hold)
            .ToListAsync();

        if (stati.Count == 0)
            throw new InvalidOperationException("Hold non trovato o scaduto.");

        foreach (var stato in stati)
        {
            stato.ScadeAtUtc = newExpiresAt;
            stato.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync();

        return new SeatHoldResponseDTO
        {
            HoldToken = holdToken,
            ScadeAtUtc = newExpiresAt,
            SalaPostoIds = stati.Select(s => s.SalaPostoId).ToList(),
            Conflitti = new List<string>()
        };
    }

    public async Task<bool> ReleaseHoldAsync(string holdToken, int userId)
    {
        var stati = await _db.ShowPostiStato
            .Where(sps => sps.HoldToken == holdToken && sps.UserId == userId && sps.Stato == ShowPostoState.Hold)
            .ToListAsync();

        if (stati.Count == 0)
            return false;

        _db.ShowPostiStato.RemoveRange(stati);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<int> CleanupExpiredHoldsAsync()
    {
        var now = DateTime.UtcNow;
        var expired = await _db.ShowPostiStato
            .Where(sps => sps.Stato == ShowPostoState.Hold && sps.ScadeAtUtc <= now)
            .ToListAsync();

        if (expired.Count > 0)
        {
            _db.ShowPostiStato.RemoveRange(expired);
            await _db.SaveChangesAsync();
        }

        return expired.Count;
    }

    private async Task CleanupExpiredHoldsForShowAsync(int showId)
    {
        var now = DateTime.UtcNow;
        var expired = await _db.ShowPostiStato
            .Where(sps => sps.ShowId == showId && sps.Stato == ShowPostoState.Hold && sps.ScadeAtUtc <= now)
            .ToListAsync();

        if (expired.Count > 0)
        {
            _db.ShowPostiStato.RemoveRange(expired);
            await _db.SaveChangesAsync();
        }
    }
}
