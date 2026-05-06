using FilmAPI.DTO;
using FilmAPI.Model;

namespace FilmAPI.Services;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO dto);
    Task<AuthResponseDTO> LoginAsync(LoginRequestDTO dto, HttpContext? httpContext = null);
    Task<AuthResponseDTO> LoginWith2FaAsync(string tempToken, string code, bool trustDevice, string? deviceId, HttpContext? httpContext = null);
    Task<AuthResponseDTO> SocialLoginAsync(User user, string? deviceId = null);
    Task<AuthResponseDTO> RefreshAsync(string refreshToken, string? deviceId);
    Task<bool> LogoutAsync(string refreshToken, string? deviceId);
    Task<UserInfoDTO?> GetUserByIdAsync(int id);

    // Password Reset
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);

    // Change / Set Password
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<bool> RequestSetPasswordAsync(int userId);
    Task<AccountSecurityDTO?> GetAccountSecurityAsync(int userId);

    // 2FA
    Task<TwoFactorSetupResponseDTO> GenerateTwoFactorSetupAsync(int userId);
    Task<bool> EnableTwoFactorAsync(int userId, string code);
    Task<bool> DisableTwoFactorAsync(int userId);
    Task<bool> VerifyTwoFactorCodeAsync(int userId, string code);
}
