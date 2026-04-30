using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class FilmCategoria
{
    [Required]
    public int FilmId { get; set; }

    [ForeignKey(nameof(FilmId))]
    public Film? Film { get; set; }

    [Required]
    public int CategoriaId { get; set; }

    [ForeignKey(nameof(CategoriaId))]
    public Categoria? Categoria { get; set; }
}
