using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

public class Cinema
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(300)]
    public string Indirizzo { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Citta { get; set; } = string.Empty;

    [Required]
    [Range(20, 500, ErrorMessage = "La capienza deve essere compresa tra 20 e 500 posti")]
    public int CapienzaTotale { get; set; } = 120;
    
    public ICollection<Proiezione> Proiezioni { get; set; } = new List<Proiezione>();
}
