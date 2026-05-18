using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Registro di soppressione notifiche della piattaforma CineBase.
/// È usato dai servizi di preferenze e deduplica per evitare che una notifica già soppressa venga ricreata o riproposta.
/// </summary>
public class NotificaSoppressa
{
    /// <summary>Identificativo univoco del record di soppressione.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Utente a cui si applica la soppressione; chiave esterna obbligatoria.</summary>
    public int UserId { get; set; }

    /// <summary>Identificativo logico della sorgente soppressa; massimo 100 caratteri.</summary>
    [Required]
    [MaxLength(100)]
    public string SourceId { get; set; } = string.Empty;

    /// <summary>Relazione con l'utente destinatario della soppressione.</summary>
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
