namespace FilmAPI.Model;

/// <summary>
/// Piano di abbonamento venduto dalla piattaforma CineBase.
/// È usato dai servizi subscription e pricing per definire benefici, rinnovi e costi nel database.
/// </summary>
public class Abbonamento
{
    /// <summary>Identificativo univoco dell'abbonamento.</summary>
    public int Id { get; set; }
    /// <summary>Nome commerciale del piano.</summary>
    public string Nome { get; set; } = string.Empty;
    /// <summary>Descrizione estesa del piano di abbonamento.</summary>
    public string Descrizione { get; set; } = string.Empty;
    /// <summary>Tipologia del piano, ad esempio mensile; valore testuale business.</summary>
    public string Tipo { get; set; } = "mensile";
    /// <summary>Prezzo del piano.</summary>
    public decimal Prezzo { get; set; }
    /// <summary>Prezzo annuale opzionale per offerte di lungo periodo.</summary>
    public decimal? PrezzoAnnuale { get; set; }
    /// <summary>Percentuale di sconto applicata rispetto al listino.</summary>
    public int ScontoPercentuale { get; set; }
    /// <summary>Numero di biglietti inclusi ogni mese.</summary>
    public int NumeroBigliettiPerMese { get; set; }
    /// <summary>Numero di popcorn inclusi ogni mese.</summary>
    public int IncludePopcornPerMese { get; set; }
    /// <summary>Indica se il piano è attivo e acquistabile.</summary>
    public bool Attivo { get; set; } = true;
    /// <summary>Data/ora UTC di creazione del piano.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
