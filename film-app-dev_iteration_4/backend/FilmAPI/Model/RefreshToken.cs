using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
}
