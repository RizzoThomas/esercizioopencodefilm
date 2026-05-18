using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Sala fisica interna a un cinema CineBase.
/// È usata dai servizi di programmazione e disponibilità posti e mappa la tabella delle sale.
/// </summary>
public class Sala
{
    /// <summary>Identificativo univoco della sala.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Cinema proprietario della sala; chiave esterna obbligatoria.</summary>
    [Required]
    public int CinemaId { get; set; }

    /// <summary>Relazione con il cinema che ospita la sala.</summary>
    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }

    /// <summary>Numero progressivo interno della sala.</summary>
    [Required]
    public int NumeroProgressivo { get; set; }

    /// <summary>Tipologia tecnica della sala; obbligatoria.</summary>
    [Required]
    public TipoSala TipoSala { get; set; }

    /// <summary>Nome opzionale della sala; massimo 100 caratteri.</summary>
    [MaxLength(100)]
    public string? Nome { get; set; }

    /// <summary>Supplemento di prezzo applicato per questa sala; importo a due decimali.</summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Supplemento { get; set; }

    /// <summary>Indica se la sala è attiva e disponibile alla programmazione.</summary>
    [Required]
    public bool IsAttiva { get; set; } = true;

    /// <summary>Posti fisici della sala.</summary>
    public ICollection<SalaPosto> Posti { get; set; } = new List<SalaPosto>();
    /// <summary>Show programmati in questa sala.</summary>
    public ICollection<Show> Shows { get; set; } = new List<Show>();
}
