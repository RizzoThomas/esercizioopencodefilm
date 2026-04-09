using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO.Auth;

public record RegisterRequestDTO(
    [Required(ErrorMessage = "L'email è obbligatoria")]
    [EmailAddress(ErrorMessage = "Formato email non valido")]
    string Email,

    [Required(ErrorMessage = "La password è obbligatoria")]
    [MinLength(6, ErrorMessage = "La password deve essere di almeno 6 caratteri")]
    string Password,

    [Required(ErrorMessage = "Il nome è obbligatorio")]
    [MaxLength(100)]
    string Nome,

    [Required(ErrorMessage = "Il cognome è obbligatorio")]
    [MaxLength(100)]
    string Cognome,

    [MaxLength(20)]
    string? Telefono,

    DateTime? DataNascita
);
