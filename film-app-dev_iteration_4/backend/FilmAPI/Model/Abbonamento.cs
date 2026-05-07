namespace FilmAPI.Model;

public class Abbonamento
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descrizione { get; set; } = string.Empty;
    public string Tipo { get; set; } = "mensile";
    public decimal Prezzo { get; set; }
    public decimal? PrezzoAnnuale { get; set; }
    public int ScontoPercentuale { get; set; }
    public int NumeroBigliettiPerMese { get; set; }
    public int IncludePopcornPerMese { get; set; }
    public bool Attivo { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
