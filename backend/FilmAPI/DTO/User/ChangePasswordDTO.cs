using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO.User;

public record ChangePasswordDTO(
    [Required(ErrorMessage = "La password attuale è obbligatoria")]
    string CurrentPassword,

    [Required(ErrorMessage = "La nuova password è obbligatoria")]
    [MinLength(6, ErrorMessage = "La nuova password deve essere di almeno 6 caratteri")]
    string NewPassword
);
