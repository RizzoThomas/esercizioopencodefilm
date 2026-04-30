namespace FilmAPI.DTO;

public class ProiezioneDTO
{
    public int Id { get; set; }
    public int CinemaId { get; set; }
    public int FilmId { get; set; }
    public DateTime Data { get; set; }
    public DateTime Ora { get; set; }
}

public class ProiezionePagedResultDTO
{
    public List<ProiezioneDTO> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class ProiezioneCreateDTO
{
    public int CinemaId { get; set; }
    public int FilmId { get; set; }
    public DateTime Data { get; set; }
    public DateTime Ora { get; set; }
}

public class ProiezioneUpdateDTO
{
    public int? CinemaId { get; set; }
    public int? FilmId { get; set; }
    public DateTime? Data { get; set; }
    public DateTime? Ora { get; set; }
}
