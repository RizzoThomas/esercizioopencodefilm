using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IProfiloService
{
    Task<UserInfoDTO?> GetProfiloAsync(int userId);
    Task<UserInfoDTO?> UpdateProfiloAsync(int userId, ProfiloUpdateDTO dto);
    Task<CinemaPreferitoDTO?> GetCinemaPreferitoAsync(int userId);
    Task<CinemaPreferitoDTO> SetCinemaPreferitoAsync(int userId, int? cinemaId);
}
