namespace FilmAPI.DTO;

public class CinemaDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Indirizzo { get; set; } = string.Empty;
    public string Citta { get; set; } = string.Empty;
}

public class CinemaPagedResultDTO
{
    public List<CinemaDTO> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class CinemaCreateDTO
{
    public string Nome { get; set; } = string.Empty;
    public string Indirizzo { get; set; } = string.Empty;
    public string Citta { get; set; } = string.Empty;
}

public class CinemaUpdateDTO
{
    public string Nome { get; set; } = string.Empty;
    public string Indirizzo { get; set; } = string.Empty;
    public string Citta { get; set; } = string.Empty;
}
