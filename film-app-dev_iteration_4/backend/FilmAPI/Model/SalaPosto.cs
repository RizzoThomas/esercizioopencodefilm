using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Posto fisico all'interno di una sala CineBase.
/// È usato dai servizi di mappa sala, prenotazione e validazione per rappresentare il singolo seggiolino nel database.
/// </summary>
public class SalaPosto
{
    /// <summary>Identificativo univoco del posto.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Sala a cui appartiene il posto; chiave esterna obbligatoria.</summary>
    [Required]
    public int SalaId { get; set; }

    /// <summary>Relazione con la sala proprietaria del posto.</summary>
    [ForeignKey(nameof(SalaId))]
    public Sala? Sala { get; set; }

    /// <summary>Settore della sala, ad esempio platea o balconata; massimo 50 caratteri.</summary>
    [Required]
    [MaxLength(50)]
    public string Settore { get; set; } = "PLATEA";

    /// <summary>Fila del posto all'interno della sala.</summary>
    [Required]
    public int Fila { get; set; }

    /// <summary>Numero progressivo del posto nella fila.</summary>
    [Required]
    public int Numero { get; set; }

    /// <summary>Coordinata X opzionale usata per mappe o layout sala.</summary>
    public int? PosX { get; set; }

    /// <summary>Coordinata Y opzionale usata per mappe o layout sala.</summary>
    public int? PosY { get; set; }

    /// <summary>Indica se il posto è accessibile in sedia a rotelle.</summary>
    [Required]
    public bool IsWheelchair { get; set; }

    /// <summary>Indica se il posto è attivo e prenotabile.</summary>
    [Required]
    public bool IsAttivo { get; set; } = true;
}
