using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

/// <summary>
/// Categoria editoriale o commerciale associata ai film di CineBase.
/// È usata dai servizi di catalogo per raggruppare i film e mappa la tabella delle categorie.
/// </summary>
public class Categoria
{
    /// <summary>Identificativo univoco della categoria.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Nome della categoria; massimo 100 caratteri.</summary>
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>Relazioni ponte con i film associati.</summary>
    public ICollection<FilmCategoria> FilmCategorie { get; set; } = new List<FilmCategoria>();
}
