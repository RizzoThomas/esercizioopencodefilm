using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Proiezione programmata nella piattaforma CineBase.
/// È usata dai servizi di programmazione, vendita e validazione e mappa la tabella delle proiezioni.
/// </summary>
public class Proiezione
{
    /// <summary>Identificativo univoco della proiezione.</summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>Cinema in cui si svolge la proiezione; chiave esterna obbligatoria.</summary>
    [Required]
    public int CinemaId { get; set; }
    
    /// <summary>Relazione con il cinema della proiezione.</summary>
    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }
    
    /// <summary>Film proiettato; chiave esterna obbligatoria.</summary>
    [Required]
    public int FilmId { get; set; }
    
    /// <summary>Relazione con il film programmato.</summary>
    [ForeignKey(nameof(FilmId))]
    public Film? Film { get; set; }
    
    /// <summary>Data della proiezione.</summary>
    [Required]
    public DateTime Data { get; set; }
    
    /// <summary>Ora della proiezione.</summary>
    [Required]
    public DateTime Ora { get; set; }
}
