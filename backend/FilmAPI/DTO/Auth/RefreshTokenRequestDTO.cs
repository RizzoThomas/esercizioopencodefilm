using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO.Auth;

public record RefreshTokenRequestDTO(
    [Required(ErrorMessage = "Il refresh token è obbligatorio")]
    string RefreshToken
);
