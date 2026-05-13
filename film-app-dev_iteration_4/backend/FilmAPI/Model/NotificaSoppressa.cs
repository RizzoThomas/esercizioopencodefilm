using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class NotificaSoppressa
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SourceId { get; set; } = string.Empty;  // "db_123", "ord_5", "prom_3"

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
