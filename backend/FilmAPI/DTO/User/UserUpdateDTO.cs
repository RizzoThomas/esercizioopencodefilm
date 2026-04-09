using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO.User;

public record UserUpdateDTO(
    [MaxLength(100)]
    string? Nome,

    [MaxLength(100)]
    string? Cognome,

    [MaxLength(20)]
    string? Telefono,

    DateTime? DataNascita
);
