using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

public class WatchlistItem
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int FilmId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Film Film { get; set; } = null!;
}
