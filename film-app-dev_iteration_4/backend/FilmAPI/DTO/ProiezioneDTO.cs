namespace FilmAPI.DTO;

/// <summary>DTO di una proiezione usato nelle API di programmazione.</summary>
public class ProiezioneDTO
{
    /// <summary>ID univoco della proiezione.</summary>
    public int Id { get; set; }
    /// <summary>ID del cinema.</summary>
    public int CinemaId { get; set; }
    /// <summary>ID del film.</summary>
    public int FilmId { get; set; }
    /// <summary>Data della proiezione.</summary>
    public DateTime Data { get; set; }
    /// <summary>Ora della proiezione.</summary>
    public DateTime Ora { get; set; }
}

/// <summary>Risultato paginato delle proiezioni.</summary>
public class ProiezionePagedResultDTO
{
    /// <summary>Elementi della pagina corrente.</summary>
    public List<ProiezioneDTO> Items { get; set; } = new();
    /// <summary>Pagina corrente.</summary>
    public int Page { get; set; }
    /// <summary>Dimensione pagina.</summary>
    public int PageSize { get; set; }
    /// <summary>Totale record.</summary>
    public int TotalCount { get; set; }
    /// <summary>Totale pagine.</summary>
    public int TotalPages { get; set; }
}

/// <summary>DTO di creazione proiezione.</summary>
public class ProiezioneCreateDTO
{
    /// <summary>ID del cinema.</summary>
    public int CinemaId { get; set; }
    /// <summary>ID del film.</summary>
    public int FilmId { get; set; }
    /// <summary>Data della proiezione.</summary>
    public DateTime Data { get; set; }
    /// <summary>Ora della proiezione.</summary>
    public DateTime Ora { get; set; }
}

/// <summary>DTO di aggiornamento proiezione.</summary>
public class ProiezioneUpdateDTO
{
    /// <summary>ID cinema opzionale.</summary>
    public int? CinemaId { get; set; }
    /// <summary>ID film opzionale.</summary>
    public int? FilmId { get; set; }
    /// <summary>Nuova data opzionale.</summary>
    public DateTime? Data { get; set; }
    /// <summary>Nuova ora opzionale.</summary>
    public DateTime? Ora { get; set; }
}
