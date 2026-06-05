using FilmAPI.Model;

namespace FilmAPI.DTO;

/// <summary>DTO di una proiezione usato nelle API di programmazione e gestione show.</summary>
public class ShowDTO
{
    /// <summary>ID univoco della proiezione.</summary>
    public int Id { get; set; }
    /// <summary>ID del cinema.</summary>
    public int CinemaId { get; set; }
    /// <summary>ID della sala.</summary>
    public int SalaId { get; set; }
    /// <summary>ID del film.</summary>
    public int FilmId { get; set; }
    /// <summary>Inizio UTC della proiezione.</summary>
    public DateTime StartAtUtc { get; set; }
    /// <summary>Snapshot della durata in minuti.</summary>
    public int DurataMinutiSnapshot { get; set; }
    /// <summary>Prezzo base della proiezione.</summary>
    public decimal PrezzoBase { get; set; }
    /// <summary>Supplemento sala.</summary>
    public decimal SupplementoSala { get; set; }
    /// <summary>Titolo del film, se presente.</summary>
    public string? FilmTitolo { get; set; }
    /// <summary>Nome del cinema, se presente.</summary>
    public string? CinemaNome { get; set; }
    /// <summary>Nome della sala, se presente.</summary>
    public string? SalaNome { get; set; }
    /// <summary>Tipo della sala, se presente.</summary>
    public TipoSala? SalaTipo { get; set; }
}

public class ShowPagedResultDTO
{
    /// <summary>Elementi della pagina corrente.</summary>
    public List<ShowDTO> Items { get; set; } = new();
    /// <summary>Pagina corrente.</summary>
    public int Page { get; set; }
    /// <summary>Dimensione pagina.</summary>
    public int PageSize { get; set; }
    /// <summary>Totale record.</summary>
    public int TotalCount { get; set; }
    /// <summary>Totale pagine.</summary>
    public int TotalPages { get; set; }
}

public class ShowCreateDTO
{
    /// <summary>ID del cinema.</summary>
    public int CinemaId { get; set; }
    /// <summary>ID della sala.</summary>
    public int SalaId { get; set; }
    /// <summary>ID del film.</summary>
    public int FilmId { get; set; }
    /// <summary>Inizio UTC della proiezione.</summary>
    public DateTime StartAtUtc { get; set; }
    /// <summary>Snapshot opzionale della durata.</summary>
    public int? DurataMinutiSnapshot { get; set; }
    /// <summary>Prezzo base opzionale.</summary>
    public decimal? PrezzoBase { get; set; }
}

public class ShowUpdateDTO
{
    /// <summary>ID cinema opzionale.</summary>
    public int? CinemaId { get; set; }
    /// <summary>ID sala opzionale.</summary>
    public int? SalaId { get; set; }
    /// <summary>ID film opzionale.</summary>
    public int? FilmId { get; set; }
    /// <summary>Nuovo inizio UTC opzionale.</summary>
    public DateTime? StartAtUtc { get; set; }
    /// <summary>Nuova durata snapshot opzionale.</summary>
    public int? DurataMinutiSnapshot { get; set; }
    /// <summary>Nuovo prezzo base opzionale.</summary>
    public decimal? PrezzoBase { get; set; }
}
