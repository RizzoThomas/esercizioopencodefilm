using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

public class Categoria
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descrizione { get; set; }

    // Navigation property
    public ICollection<FilmCategoria> FilmCategorie { get; set; } = new List<FilmCategoria>();
}

public class FilmCategoria
{
    public int FilmId { get; set; }
    public Film Film { get; set; } = null!;

    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
}
