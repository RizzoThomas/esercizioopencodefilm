namespace CognomeNomeAPI.Model;

public class Regista
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public string Cognome { get; set; } = null!;
    public string Nazionalita { get; set; } = null!;
    public List<Film>? Films { get; set; }
}
