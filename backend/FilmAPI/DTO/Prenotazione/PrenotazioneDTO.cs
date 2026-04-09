using FilmAPI.Model;
using FilmAPI.DTO.UserProiezione;

namespace FilmAPI.DTO.Prenotazione;

public record PrenotazioneDTO(
    int Id,
    string CodicePrenotazione,
    FilmSummaryDTO Film,
    CinemaSummaryDTO Cinema,
    DateTime DataProiezione,
    TimeSpan OraProiezione,
    int PostiDisponibili,
    int CapienzaCinema,
    int NumeroPosti,
    string[] Posti,
    decimal? PrezzoTotale,
    StatoPrenotazione Stato,
    DateTime CreatedAt
);
