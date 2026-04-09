using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO.Categoria;

public record CategoriaCreateDTO(
    [Required(ErrorMessage = "Il nome della categoria è obbligatorio")]
    [MaxLength(100)]
    string Nome,

    [MaxLength(500)]
    string? Descrizione
);
