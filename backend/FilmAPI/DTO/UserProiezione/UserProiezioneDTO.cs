namespace FilmAPI.DTO.UserProiezione;

public record UserProiezioneDTO(
    int Id,
    int ProiezioneId,
    FilmSummaryDTO Film,
    CinemaSummaryDTO Cinema,
    DateTime DataProiezione,
    TimeSpan OraProiezione,
    DateTime SavedAt,
    string? Note
);

public record FilmSummaryDTO(
    int Id,
    string Titolo,
    string? CopertinaPath
);

public record CinemaSummaryDTO(
    int Id,
    string Nome,
    string Citta
);
