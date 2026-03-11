namespace CognomeNomeAPI.DTO;

public class RegistaDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public string Cognome { get; set; } = null!;
    public string Nazionalita { get; set; } = null!;
}
