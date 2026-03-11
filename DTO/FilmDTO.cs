namespace CognomeNomeAPI.DTO;

public class FilmDTO
{
    public int Id { get; set; }
    public string Titolo { get; set; } = null!;
    public DateTime DataProduzione { get; set; }
    public int RegistaId { get; set; }
    public int Durata { get; set; }
}
