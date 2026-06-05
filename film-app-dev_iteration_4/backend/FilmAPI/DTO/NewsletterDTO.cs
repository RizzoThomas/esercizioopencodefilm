using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO;

/// <summary>DTO di richiesta iscrizione newsletter usato nelle API di subscription.</summary>
public class NewsletterSubscribeDTO
{
    /// <summary>Email da iscrivere; è obbligatoria e deve avere formato valido.</summary>
    [Required(ErrorMessage = "L'email è obbligatoria.")]
    [EmailAddress(ErrorMessage = "Formato email non valido.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Nome opzionale dell'iscritto; serve per personalizzare le comunicazioni.</summary>
    [MaxLength(128)]
    public string? Nome { get; set; }
}

/// <summary>DTO di risposta per l'esito di iscrizione newsletter.</summary>
public class NewsletterResponseDTO
{
    /// <summary>Indica se l'operazione è andata a buon fine.</summary>
    public bool Success { get; set; }
    /// <summary>Messaggio descrittivo dell'esito.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Indica se è stata inviata l'email di benvenuto.</summary>
    public bool WelcomeEmailSent { get; set; }
}
