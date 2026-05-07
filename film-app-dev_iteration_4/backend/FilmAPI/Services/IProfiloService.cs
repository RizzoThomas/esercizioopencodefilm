using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IProfiloService
{
    Task<UserInfoDTO?> GetProfiloAsync(int userId);
    Task<UserInfoDTO?> UpdateProfiloAsync(int userId, ProfiloUpdateDTO dto);
    Task<CinemaPreferitoDTO?> GetCinemaPreferitoAsync(int userId);
    Task<CinemaPreferitoDTO> SetCinemaPreferitoAsync(int userId, int? cinemaId);
    Task<UserSubscriptionDTO?> GetUserSubscriptionAsync(int userId);
    Task<List<UserVoucherDTO>> GetUserVouchersAsync(int userId);
    Task<UserSubscriptionDTO?> CancelUserSubscriptionAsync(int userId);
    Task<UserSubscriptionDTO?> ToggleAutoRenewAsync(int userId, bool autoRinnovo);
}
