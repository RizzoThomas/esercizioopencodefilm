namespace FilmAPI.Model;

public class Offerta
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descrizione { get; set; } = string.Empty;
    public string Tipo { get; set; } = "solo_biglietti";
    public decimal Prezzo { get; set; }
    public decimal? PrezzoOriginale { get; set; }
    public int ScontoPercentuale { get; set; }
    public bool InEvidenza { get; set; }
    public int NumeroBiglietti { get; set; }
    public int IncludePopcorn { get; set; }
    public int? CinemaId { get; set; }
    public Cinema? Cinema { get; set; }
    public bool Attiva { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
