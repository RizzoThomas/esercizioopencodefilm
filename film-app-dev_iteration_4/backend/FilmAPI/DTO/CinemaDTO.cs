namespace FilmAPI.DTO;

/// <summary>DTO di un cinema usato nelle API di elenco e gestione sedi.</summary>
public class CinemaDTO
{
    /// <summary>ID univoco del cinema.</summary>
    public int Id { get; set; }
    /// <summary>Nome del cinema.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Indirizzo del cinema.</summary>
    public string Indirizzo { get; set; } = string.Empty;
    /// <summary>Città del cinema.</summary>
    public string Citta { get; set; } = string.Empty;
}

/// <summary>Risultato paginato dei cinema.</summary>
public class CinemaPagedResultDTO
{
    /// <summary>Elementi della pagina corrente.</summary>
    public List<CinemaDTO> Items { get; set; } = new();
    /// <summary>Numero pagina corrente.</summary>
    public int Page { get; set; }
    /// <summary>Dimensione pagina.</summary>
    public int PageSize { get; set; }
    /// <summary>Totale record.</summary>
    public int TotalCount { get; set; }
    /// <summary>Totale pagine.</summary>
    public int TotalPages { get; set; }
}

/// <summary>DTO di creazione cinema; usato nelle API POST.</summary>
public class CinemaCreateDTO
{
    /// <summary>Nome del nuovo cinema.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Indirizzo del nuovo cinema.</summary>
    public string Indirizzo { get; set; } = string.Empty;
    /// <summary>Città del nuovo cinema.</summary>
    public string Citta { get; set; } = string.Empty;
}

/// <summary>DTO di aggiornamento cinema; usato nelle API PUT/PATCH.</summary>
public class CinemaUpdateDTO
{
    /// <summary>Nome aggiornato del cinema.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Indirizzo aggiornato del cinema.</summary>
    public string Indirizzo { get; set; } = string.Empty;
    /// <summary>Città aggiornata del cinema.</summary>
    public string Citta { get; set; } = string.Empty;
}
