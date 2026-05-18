namespace FilmAPI.DTO;

/// <summary>DTO film usato nelle API di catalogo e gestione film.</summary>
public class FilmDTO
{
    /// <summary>ID univoco del film.</summary>
    public int Id { get; set; }
    /// <summary>Titolo del film.</summary>
    public string Titolo { get; set; } = string.Empty;
    /// <summary>Data di produzione.</summary>
    public DateTime DataProduzione { get; set; }
    /// <summary>ID del regista associato.</summary>
    public int RegistaId { get; set; }
    /// <summary>Nome del regista, se disponibile.</summary>
    public string? RegistaNome { get; set; }
    /// <summary>Cognome del regista, se disponibile.</summary>
    public string? RegistaCognome { get; set; }
    /// <summary>Durata del film in minuti.</summary>
    public int Durata { get; set; }
    /// <summary>Path della copertina.</summary>
    public string? CopertinaPath { get; set; }
    /// <summary>Path del filmato, se presente.</summary>
    public string? FilmatoPath { get; set; }
    /// <summary>Descrizione lunga del film.</summary>
    public string? DescrizioneLunga { get; set; }
    /// <summary>Testo cast aggregato.</summary>
    public string? CastText { get; set; }
    /// <summary>Data di rilascio opzionale.</summary>
    public DateOnly? DataRilascio { get; set; }
    /// <summary>Voto medio del film, se calcolato.</summary>
    public double? VoteAverage { get; set; }
    /// <summary>Categorie associate al film.</summary>
    public List<CategoriaDTO> Categorie { get; set; } = new();
}

public class FilmPagedResultDTO
{
    /// <summary>Elementi della pagina corrente.</summary>
    public List<FilmDTO> Items { get; set; } = new();
    /// <summary>Pagina corrente.</summary>
    public int Page { get; set; }
    /// <summary>Dimensione pagina.</summary>
    public int PageSize { get; set; }
    /// <summary>Totale record.</summary>
    public int TotalCount { get; set; }
    /// <summary>Totale pagine.</summary>
    public int TotalPages { get; set; }
}

public class FilmCreateDTO
{
    /// <summary>Titolo del nuovo film.</summary>
    public string Titolo { get; set; } = string.Empty;
    /// <summary>Data di produzione.</summary>
    public DateTime DataProduzione { get; set; }
    /// <summary>ID del regista da associare.</summary>
    public int RegistaId { get; set; }
    /// <summary>Durata in minuti.</summary>
    public int Durata { get; set; }
    /// <summary>Copertina opzionale.</summary>
    public string? CopertinaPath { get; set; }
    /// <summary>Filmato opzionale.</summary>
    public string? FilmatoPath { get; set; }
    /// <summary>Descrizione lunga opzionale.</summary>
    public string? DescrizioneLunga { get; set; }
    /// <summary>Testo cast opzionale.</summary>
    public string? CastText { get; set; }
    /// <summary>Data di rilascio opzionale.</summary>
    public DateOnly? DataRilascio { get; set; }
    /// <summary>Lista di categorie da associare.</summary>
    public List<int>? CategorieIds { get; set; }
}

public class FilmUpdateDTO
{
    /// <summary>Titolo aggiornato del film.</summary>
    public string Titolo { get; set; } = string.Empty;
    /// <summary>Data di produzione aggiornata.</summary>
    public DateTime DataProduzione { get; set; }
    /// <summary>ID del regista aggiornato.</summary>
    public int RegistaId { get; set; }
    /// <summary>Durata aggiornata in minuti.</summary>
    public int Durata { get; set; }
    /// <summary>Copertina aggiornata, se presente.</summary>
    public string? CopertinaPath { get; set; }
    /// <summary>Filmato aggiornato, se presente.</summary>
    public string? FilmatoPath { get; set; }
    /// <summary>Descrizione lunga aggiornata, se presente.</summary>
    public string? DescrizioneLunga { get; set; }
    /// <summary>Testo cast aggiornato, se presente.</summary>
    public string? CastText { get; set; }
    /// <summary>Data di rilascio aggiornata, se presente.</summary>
    public DateOnly? DataRilascio { get; set; }
    /// <summary>Lista categorie aggiornata.</summary>
    public List<int>? CategorieIds { get; set; }
}
