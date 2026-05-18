namespace FilmAPI.Model;

/// <summary>
/// Offerta commerciale promozionale della piattaforma CineBase.
/// È usata dai servizi marketing e vendita per definire pacchetti scontati e mappa la tabella delle offerte.
/// </summary>
public class Offerta
{
    /// <summary>Identificativo univoco dell'offerta.</summary>
    public int Id { get; set; }
    /// <summary>Nome commerciale dell'offerta; valore libero usato in UI e catalogo.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Descrizione estesa dell'offerta.</summary>
    public string Descrizione { get; set; } = string.Empty;
    /// <summary>Tipologia di offerta, ad esempio solo biglietti; valore testuale business.</summary>
    public string Tipo { get; set; } = "solo_biglietti";
    /// <summary>Prezzo finale dell'offerta.</summary>
    public decimal Prezzo { get; set; }
    /// <summary>Prezzo originale prima dello sconto, se esposto in catalogo.</summary>
    public decimal? PrezzoOriginale { get; set; }
    /// <summary>Percentuale di sconto applicata.</summary>
    public int ScontoPercentuale { get; set; }
    /// <summary>Indica se l'offerta è evidenziata nella UI.</summary>
    public bool InEvidenza { get; set; }
    /// <summary>Numero di biglietti inclusi nell'offerta.</summary>
    public int NumeroBiglietti { get; set; }
    /// <summary>Quantità di popcorn inclusi nell'offerta.</summary>
    public int IncludePopcorn { get; set; }
    /// <summary>Cinema di appartenenza dell'offerta, se limitata a una sede.</summary>
    public int? CinemaId { get; set; }
    /// <summary>Relazione con il cinema associato all'offerta.</summary>
    public Cinema? Cinema { get; set; }
    /// <summary>Indica se l'offerta è attiva nel catalogo.</summary>
    public bool Attiva { get; set; } = true;
    /// <summary>Data/ora UTC di creazione dell'offerta.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
