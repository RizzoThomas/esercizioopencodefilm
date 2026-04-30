using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface ISeatHoldService
{
    Task<SeatMapDTO> GetSeatMapAsync(int showId, int userId);
    Task<SeatHoldResponseDTO> CreateHoldAsync(int showId, int userId, List<int> salaPostoIds);
    Task<SeatHoldResponseDTO> RefreshHoldAsync(string holdToken, int userId);
    Task<bool> ReleaseHoldAsync(string holdToken, int userId);
    Task<int> CleanupExpiredHoldsAsync();
}
