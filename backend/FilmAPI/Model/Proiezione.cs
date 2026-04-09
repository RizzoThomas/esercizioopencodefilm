using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class Proiezione
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int CinemaId { get; set; }
    
    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }
    
    [Required]
    public int FilmId { get; set; }
    
    [ForeignKey(nameof(FilmId))]
    public Film? Film { get; set; }
    
    [Required]
    public DateTime Data { get; set; }
    
    [Required]
    public DateTime Ora { get; set; }
}
