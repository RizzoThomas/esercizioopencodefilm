namespace FilmAPI.DTO;

public class AbbonamentoDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descrizione { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal Prezzo { get; set; }
    public decimal? PrezzoAnnuale { get; set; }
    public int ScontoPercentuale { get; set; }
    public int NumeroBigliettiPerMese { get; set; }
    public int IncludePopcornPerMese { get; set; }
}

public class AttivaAbbonamentoRequest
{
    public string? MetodoPagamento { get; set; }
    public bool AutoRinnovo { get; set; } = true;
}
