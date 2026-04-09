using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO.UserProiezione;

public record UserProiezioneCreateDTO(
    [Required(ErrorMessage = "L'ID della proiezione è obbligatorio")]
    int ProiezioneId,

    [MaxLength(500)]
    string? Note
);
