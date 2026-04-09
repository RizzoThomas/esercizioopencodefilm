using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

public class Regista
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Nazionalita { get; set; } = string.Empty;
    
    public ICollection<Film> Films { get; set; } = new List<Film>();
}
