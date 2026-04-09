namespace FilmAPI.DTO.Prenotazione;

public record PrenotazioneDisponibilitaDTO(
    int ProiezioneId,
    int CinemaId,
    string CinemaNome,
    string CinemaCitta,
    int FilmId,
    string FilmTitolo,
    string? FilmCopertinaPath,
    DateTime DataProiezione,
    TimeSpan OraProiezione,
    int CapienzaCinema,
    int PostiPrenotati,
    int PostiDisponibili,
    int MaxPostiPrenotabili,
    string[] PostiOccupati,
    string[] TuttiIPosti
);
