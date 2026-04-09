using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    public DateTime? DataNascita { get; set; }

    [Required]
    public UserRole Ruolo { get; set; } = UserRole.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserProiezione> ProiezioniSalvate { get; set; } = new List<UserProiezione>();
    public ICollection<Prenotazione> Prenotazioni { get; set; } = new List<Prenotazione>();
}

public enum UserRole
{
    Admin = 0,
    PowerUser = 1,
    User = 2
}
