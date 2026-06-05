using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

/// <summary>
/// Prenotazione legacy collegata a una proiezione CineBase.
/// È usata dal flusso storico di prenotazione e mappa la tabella delle prenotazioni.
/// </summary>
public class Prenotazione
{
    /// <summary>Identificativo univoco della prenotazione.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Utente che ha effettuato la prenotazione; chiave esterna obbligatoria.</summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>Relazione con l'utente prenotante.</summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>Proiezione prenotata; chiave esterna obbligatoria.</summary>
    [Required]
    public int ProiezioneId { get; set; }

    /// <summary>Relazione con la proiezione associata.</summary>
    [ForeignKey(nameof(ProiezioneId))]
    public Proiezione? Proiezione { get; set; }

    /// <summary>Numero di posti prenotati.</summary>
    [Required]
    public int NumeroPosti { get; set; }

    /// <summary>Note opzionali della prenotazione; massimo 500 caratteri.</summary>
    [MaxLength(500)]
    public string? Note { get; set; }

    /// <summary>Data/ora della prenotazione.</summary>
    [Required]
    public DateTime DataPrenotazione { get; set; }
}
