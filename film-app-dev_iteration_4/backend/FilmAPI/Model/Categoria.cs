using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

public class Categoria
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public ICollection<FilmCategoria> FilmCategorie { get; set; } = new List<FilmCategoria>();
}
