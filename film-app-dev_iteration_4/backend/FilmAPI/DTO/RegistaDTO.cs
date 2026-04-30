namespace FilmAPI.DTO;

public class RegistaDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string Nazionalita { get; set; } = string.Empty;
}

public class RegistaPagedResultDTO
{
    public List<RegistaDTO> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class RegistaCreateDTO
{
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string Nazionalita { get; set; } = string.Empty;
}

public class RegistaUpdateDTO
{
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string Nazionalita { get; set; } = string.Empty;
}
