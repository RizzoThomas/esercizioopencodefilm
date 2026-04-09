using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO.Auth;

public record LoginRequestDTO(
    [Required(ErrorMessage = "L'email è obbligatoria")]
    [EmailAddress(ErrorMessage = "Formato email non valido")]
    string Email,

    [Required(ErrorMessage = "La password è obbligatoria")]
    [MinLength(6, ErrorMessage = "La password deve essere di almeno 6 caratteri")]
    string Password
);
