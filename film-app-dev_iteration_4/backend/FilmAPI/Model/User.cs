using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [Required]
    public UserRole Ruolo { get; set; }

    [Required]
    public DateTime DataRegistrazione { get; set; }

    public int? CinemaPreferitoId { get; set; }

    [ForeignKey(nameof(CinemaPreferitoId))]
    public Cinema? CinemaPreferito { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CreditoResiduo { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Prenotazione> Prenotazioni { get; set; } = new List<Prenotazione>();
    public ICollection<Ordine> Ordini { get; set; } = new List<Ordine>();
    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();

    // Password Reset
    [MaxLength(128)]
    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    // 2FA
    [MaxLength(64)]
    public string? TwoFactorSecret { get; set; }
    public bool TwoFactorEnabled { get; set; }
}