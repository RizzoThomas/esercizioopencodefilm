namespace FilmAPI.DTO;

/// <summary>DTO di sintesi film per la programmazione; usato nelle API di elenco film in sala.</summary>
public class ProgrammazioneFilmDTO
{
    /// <summary>ID univoco del film.</summary>
    public int Id { get; set; }
    /// <summary>Titolo del film; serve per l'elenco programmazione.</summary>
    public string Titolo { get; set; } = string.Empty;
    /// <summary>Path della copertina; serve per mostrare la locandina.</summary>
    public string? CopertinaPath { get; set; }
    /// <summary>Durata del film in minuti.</summary>
    public int Durata { get; set; }
    /// <summary>Categorie associate; servono per filtraggio e navigazione.</summary>
    public List<CategoriaDTO> Categorie { get; set; } = new();
    /// <summary>Data di rilascio opzionale.</summary>
    public DateOnly? DataRilascio { get; set; }
    /// <summary>Indica se il film è in evidenza.</summary>
    public bool InEvidenza { get; set; }
    /// <summary>Indica se il film è in uscita.</summary>
    public bool InUscita { get; set; }
    /// <summary>Numero di show nei prossimi 7 giorni.</summary>
    public int ShowCountNext7Days { get; set; }
    /// <summary>Indica se è disponibile nel cinema selezionato.</summary>
    public bool DisponibileNelCinemaSelezionato { get; set; }
    /// <summary>Data/ora del prossimo show nel cinema selezionato, se presente.</summary>
    public DateTime? ProssimoShowNelCinemaSelezionato { get; set; }
}

/// <summary>Risultato paginato dei film di programmazione.</summary>
public class ProgrammazioneFilmPagedResultDTO
{
    /// <summary>Elementi della pagina corrente.</summary>
    public List<ProgrammazioneFilmDTO> Items { get; set; } = new();
    /// <summary>Numero pagina.</summary>
    public int Page { get; set; }
    /// <summary>Dimensione pagina.</summary>
    public int PageSize { get; set; }
    /// <summary>Totale record.</summary>
    public int TotalCount { get; set; }
    /// <summary>Totale pagine.</summary>
    public int TotalPages { get; set; }
    /// <summary>Indica se esiste una pagina successiva.</summary>
    public bool HasNextPage { get; set; }
    /// <summary>Indica se esiste una pagina precedente.</summary>
    public bool HasPreviousPage { get; set; }
}

/// <summary>DTO della scheda film usato nelle API di dettaglio e orari.</summary>
public class FilmSchedaDTO
{
    /// <summary>ID univoco del film.</summary>
    public int Id { get; set; }
    /// <summary>Titolo del film.</summary>
    public string Titolo { get; set; } = string.Empty;
    /// <summary>Path della copertina.</summary>
    public string? CopertinaPath { get; set; }
    /// <summary>Path del filmato, se presente.</summary>
    public string? FilmatoPath { get; set; }
    /// <summary>Durata del film in minuti.</summary>
    public int Durata { get; set; }
    /// <summary>Data di produzione; serve per il dettaglio anagrafico del film.</summary>
    public DateTime DataProduzione { get; set; }
    /// <summary>Data di rilascio opzionale.</summary>
    public DateOnly? DataRilascio { get; set; }
    /// <summary>Descrizione estesa del film.</summary>
    public string? DescrizioneLunga { get; set; }
    /// <summary>Testo cast aggregato.</summary>
    public string? CastText { get; set; }
    /// <summary>Lista cast normalizzata.</summary>
    public List<string> CastList { get; set; } = new();
    /// <summary>Categorie del film.</summary>
    public List<CategoriaDTO> Categorie { get; set; } = new();
    /// <summary>Nome del regista.</summary>
    public string? RegistaNome { get; set; }
    /// <summary>Cognome del regista.</summary>
    public string? RegistaCognome { get; set; }
    /// <summary>Cinema selezionato nel contesto della scheda.</summary>
    public CinemaSintesiDTO? CinemaSelezionato { get; set; }
    /// <summary>Calendario degli show raggruppati per data.</summary>
    public List<FilmSchedaShowGroupDTO> ShowCalendar { get; set; } = new();
}

/// <summary>Gruppo di show della scheda film per una data specifica.</summary>
public class FilmSchedaShowGroupDTO
{
    /// <summary>Data del gruppo di show.</summary>
    public DateOnly Data { get; set; }
    /// <summary>Raggruppamenti per tipo sala.</summary>
    public List<FilmSchedaTipoSalaGroupDTO> GruppiPerTipoSala { get; set; } = new();
}

/// <summary>Raggruppamento degli show per tipologia di sala.</summary>
public class FilmSchedaTipoSalaGroupDTO
{
    /// <summary>Tipo sala del gruppo.</summary>
    public string TipoSala { get; set; } = string.Empty;
    /// <summary>Show disponibili per quel tipo sala.</summary>
    public List<FilmSchedaShowItemDTO> Shows { get; set; } = new();
}

/// <summary>Item singolo dello show nella scheda film.</summary>
public class FilmSchedaShowItemDTO
{
    /// <summary>ID univoco dello show.</summary>
    public int ShowId { get; set; }
    /// <summary>Data/ora UTC di inizio.</summary>
    public DateTime StartAtUtc { get; set; }
    /// <summary>Prezzo base dello show.</summary>
    public decimal PrezzoBase { get; set; }
    /// <summary>Supplemento sala.</summary>
    public decimal SupplementoSala { get; set; }
    /// <summary>ID della sala.</summary>
    public int SalaId { get; set; }
    /// <summary>Nome della sala, se disponibile.</summary>
    public string? SalaNome { get; set; }
    /// <summary>Numero progressivo della sala.</summary>
    public int SalaNumeroProgressivo { get; set; }
}

/// <summary>DTO di card cinema usato nelle liste e ricerche cinema.</summary>
public class CinemaCardDTO
{
    /// <summary>ID univoco del cinema.</summary>
    public int Id { get; set; }
    /// <summary>Nome del cinema.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Città del cinema.</summary>
    public string Citta { get; set; } = string.Empty;
    /// <summary>Indirizzo del cinema.</summary>
    public string Indirizzo { get; set; } = string.Empty;
    /// <summary>Tipologie di sale presenti.</summary>
    public List<string> TipologieSalePresenti { get; set; } = new();
    /// <summary>Distanza in chilometri, se calcolata.</summary>
    public double? DistanzaKm { get; set; }
    /// <summary>Latitudine del cinema, se disponibile.</summary>
    public double? Latitudine { get; set; }
    /// <summary>Longitudine del cinema, se disponibile.</summary>
    public double? Longitudine { get; set; }
}

/// <summary>DTO del palinsesto giornaliero di un cinema.</summary>
public class CinemaScheduleDayDTO
{
    /// <summary>Riepilogo del cinema associato al giorno.</summary>
    public CinemaSintesiDTO Cinema { get; set; } = new();
    /// <summary>Data del palinsesto.</summary>
    public DateOnly Data { get; set; }
    /// <summary>Film programmati in quel giorno.</summary>
    public List<CinemaScheduleFilmDTO> Films { get; set; } = new();
}

/// <summary>DTO di sintesi film per il palinsesto di un cinema.</summary>
public class CinemaScheduleFilmDTO
{
    /// <summary>ID del film.</summary>
    public int FilmId { get; set; }
    /// <summary>Titolo del film.</summary>
    public string Titolo { get; set; } = string.Empty;
    /// <summary>Path della copertina.</summary>
    public string? CopertinaPath { get; set; }
    /// <summary>Estratto descrittivo del film.</summary>
    public string? DescrizioneEstratto { get; set; }
    /// <summary>Raggruppamenti show per tipologia sala.</summary>
    public List<CinemaScheduleTipoSalaGroupDTO> GruppiPerTipoSala { get; set; } = new();
}

/// <summary>Raggruppamento degli show nel palinsesto per tipologia sala.</summary>
public class CinemaScheduleTipoSalaGroupDTO
{
    /// <summary>Tipo della sala.</summary>
    public string TipoSala { get; set; } = string.Empty;
    /// <summary>Show del tipo sala.</summary>
    public List<CinemaScheduleShowItemDTO> Shows { get; set; } = new();
}

/// <summary>Item singolo dello show nel palinsesto del cinema.</summary>
public class CinemaScheduleShowItemDTO
{
    /// <summary>ID dello show.</summary>
    public int ShowId { get; set; }
    /// <summary>Data/ora UTC di inizio.</summary>
    public DateTime StartAtUtc { get; set; }
    /// <summary>ID della sala.</summary>
    public int SalaId { get; set; }
    /// <summary>Nome della sala.</summary>
    public string? SalaNome { get; set; }
    /// <summary>Numero progressivo della sala.</summary>
    public int SalaNumeroProgressivo { get; set; }
}

/// <summary>DTO sintetico del cinema usato nelle API di dettaglio e ricerca.</summary>
public class CinemaSintesiDTO
{
    /// <summary>ID univoco del cinema.</summary>
    public int Id { get; set; }
    /// <summary>Nome del cinema.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Città del cinema.</summary>
    public string Citta { get; set; } = string.Empty;
    /// <summary>Indirizzo del cinema.</summary>
    public string Indirizzo { get; set; } = string.Empty;
    /// <summary>Telefono opzionale del cinema.</summary>
    public string? Telefono { get; set; }
    /// <summary>Codice locale opzionale.</summary>
    public string? CodiceLocale { get; set; }
}

/// <summary>DTO del cinema preferito di un utente.</summary>
public class CinemaPreferitoDTO
{
    /// <summary>ID del cinema preferito, se impostato.</summary>
    public int? CinemaId { get; set; }
    /// <summary>Dettaglio del cinema preferito, se disponibile.</summary>
    public CinemaSintesiDTO? Cinema { get; set; }
}
