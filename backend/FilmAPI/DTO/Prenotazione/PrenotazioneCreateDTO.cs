using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO.Prenotazione;

public record PrenotazioneCreateDTO(
    [Required(ErrorMessage = "L'ID della proiezione è obbligatorio")]
    int ProiezioneId,

    [Required(ErrorMessage = "Il numero di posti è obbligatorio")]
    [Range(1, 20, ErrorMessage = "Il numero di posti deve essere tra 1 e 20")]
    int NumeroPosti,

    string[]? Posti
);
