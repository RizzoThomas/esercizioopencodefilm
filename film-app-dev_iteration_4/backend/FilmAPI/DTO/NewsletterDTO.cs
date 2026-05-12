using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO;

public class NewsletterSubscribeDTO
{
    [Required(ErrorMessage = "L'email è obbligatoria.")]
    [EmailAddress(ErrorMessage = "Formato email non valido.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Nome { get; set; }
}

public class NewsletterResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool WelcomeEmailSent { get; set; }
}
