namespace FilmAPI.DTO;

/// <summary>DTO di un regista usato nelle API di catalogo film.</summary>
public class RegistaDTO
{
    /// <summary>ID univoco del regista.</summary>
    public int Id { get; set; }
    /// <summary>Nome del regista.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Cognome del regista.</summary>
    public string Cognome { get; set; } = string.Empty;
    /// <summary>Nazionalità del regista.</summary>
    public string Nazionalita { get; set; } = string.Empty;
}

/// <summary>Risultato paginato dei registi.</summary>
public class RegistaPagedResultDTO
{
    /// <summary>Elementi della pagina corrente.</summary>
    public List<RegistaDTO> Items { get; set; } = new();
    /// <summary>Numero pagina corrente.</summary>
    public int Page { get; set; }
    /// <summary>Dimensione pagina.</summary>
    public int PageSize { get; set; }
    /// <summary>Totale record.</summary>
    public int TotalCount { get; set; }
    /// <summary>Totale pagine.</summary>
    public int TotalPages { get; set; }
}

/// <summary>DTO di creazione regista; usato nelle API POST.</summary>
public class RegistaCreateDTO
{
    /// <summary>Nome del regista.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Cognome del regista.</summary>
    public string Cognome { get; set; } = string.Empty;
    /// <summary>Nazionalità del regista.</summary>
    public string Nazionalita { get; set; } = string.Empty;
}

/// <summary>DTO di aggiornamento regista; usato nelle API PUT/PATCH.</summary>
public class RegistaUpdateDTO
{
    /// <summary>Nome aggiornato del regista.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Cognome aggiornato del regista.</summary>
    public string Cognome { get; set; } = string.Empty;
    /// <summary>Nazionalità aggiornata del regista.</summary>
    public string Nazionalita { get; set; } = string.Empty;
}
