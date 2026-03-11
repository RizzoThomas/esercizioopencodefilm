namespace CognomeNomeAPI.DTO;

public class ProiezioneViewDTO
{
    public int Id { get; set; }
    public int FilmId { get; set; }
    public string FilmTitolo { get; set; } = null!;
    public int CinemaId { get; set; }
    public string CinemaNome { get; set; } = null!;
    public DateTime Data { get; set; }
    public DateTime Ora { get; set; }
}
