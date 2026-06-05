using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

/// <summary>
/// Anagrafica del cinema fisico gestito dalla piattaforma CineBase.
/// È usata dai servizi di programmazione, disponibilità sale e ordini e corrisponde alla tabella dei cinema.
/// </summary>
public class Cinema
{
    /// <summary>Identificativo univoco del cinema.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Nome pubblico del cinema; massimo 200 caratteri.</summary>
    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>Indirizzo completo della sede; massimo 300 caratteri.</summary>
    [Required]
    [MaxLength(300)]
    public string Indirizzo { get; set; } = string.Empty;

    /// <summary>Città in cui si trova il cinema; massimo 100 caratteri.</summary>
    [Required]
    [MaxLength(100)]
    public string Citta { get; set; } = string.Empty;

    /// <summary>Latitudine geografica opzionale.</summary>
    public double? Latitudine { get; set; }

    /// <summary>Longitudine geografica opzionale.</summary>
    public double? Longitudine { get; set; }

    /// <summary>Telefono di contatto; massimo 20 caratteri.</summary>
    [MaxLength(20)]
    public string? Telefono { get; set; }

    /// <summary>Codice locale usato dai servizi interni; massimo 50 caratteri.</summary>
    [MaxLength(50)]
    public string? CodiceLocale { get; set; }

    /// <summary>Proiezioni programmate nel cinema.</summary>
    public ICollection<Proiezione> Proiezioni { get; set; } = new List<Proiezione>();

    /// <summary>Sale appartenenti al cinema.</summary>
    public ICollection<Sala> Sale { get; set; } = new List<Sala>();
}
