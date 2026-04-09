using FilmAPI.DTO.User;

namespace FilmAPI.DTO.Auth;

public record LoginResponseDTO(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDTO User
);
