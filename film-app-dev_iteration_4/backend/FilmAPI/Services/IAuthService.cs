using FilmAPI.DTO;

namespace FilmAPI.Services;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO dto);
    Task<AuthResponseDTO> LoginAsync(LoginRequestDTO dto);
    Task<AuthResponseDTO> RefreshAsync(string refreshToken, string? deviceId);
    Task<bool> LogoutAsync(string refreshToken, string? deviceId);
    Task<UserInfoDTO?> GetUserByIdAsync(int id);
}
