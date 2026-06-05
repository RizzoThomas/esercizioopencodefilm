namespace FilmAPI.DTO;

/// <summary>DTO di un abbonamento esposto dalle API di catalogo.</summary>
public class AbbonamentoDTO
{
    /// <summary>ID univoco dell'abbonamento.</summary>
    public int Id { get; set; }
    /// <summary>Nome dell'abbonamento.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Descrizione dell'abbonamento.</summary>
    public string Descrizione { get; set; } = string.Empty;
    /// <summary>Tipo dell'abbonamento.</summary>
    public string Tipo { get; set; } = string.Empty;
    /// <summary>Prezzo mensile.</summary>
    public decimal Prezzo { get; set; }
    /// <summary>Prezzo annuale opzionale.</summary>
    public decimal? PrezzoAnnuale { get; set; }
    /// <summary>Sconto percentuale applicato.</summary>
    public int ScontoPercentuale { get; set; }
    /// <summary>Numero biglietti mensili inclusi.</summary>
    public int NumeroBigliettiPerMese { get; set; }
    /// <summary>Numero popcorn mensili inclusi.</summary>
    public int IncludePopcornPerMese { get; set; }
}

/// <summary>DTO di richiesta attivazione abbonamento.</summary>
public class AttivaAbbonamentoRequest
{
    /// <summary>Metodo di pagamento opzionale.</summary>
    public string? MetodoPagamento { get; set; }
    /// <summary>Indica se il rinnovo automatico è attivo.</summary>
    public bool AutoRinnovo { get; set; } = true;
}
