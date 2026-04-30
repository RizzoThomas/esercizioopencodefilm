namespace FilmAPI.DTO;

public class CategoriaDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
}

public class CategoriaCreateDTO
{
    public string Nome { get; set; } = string.Empty;
}

public class CategoriaUpdateDTO
{
    public string Nome { get; set; } = string.Empty;
}
