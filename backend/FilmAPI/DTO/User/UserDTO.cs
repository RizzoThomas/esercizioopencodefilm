namespace FilmAPI.DTO.User;

public record UserDTO(
    int Id,
    string Email,
    string Nome,
    string Cognome,
    string? Telefono,
    DateTime? DataNascita,
    string Ruolo,
    DateTime CreatedAt
);
