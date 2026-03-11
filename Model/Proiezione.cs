namespace CognomeNomeAPI.Model;

public class Proiezione
{
    public int Id { get; set; }
    public int CinemaId { get; set; }
    public int FilmId { get; set; }
    public DateTime Data { get; set; }
    public DateTime Ora { get; set; }
}
