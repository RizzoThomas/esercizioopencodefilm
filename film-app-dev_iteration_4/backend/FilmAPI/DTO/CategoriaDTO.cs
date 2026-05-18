namespace FilmAPI.DTO;

/// <summary>DTO categoria usato nelle API di catalogo film.</summary>
public class CategoriaDTO
{
    /// <summary>ID univoco della categoria.</summary>
    public int Id { get; set; }
    /// <summary>Nome della categoria.</summary>
    public string Nome { get; set; } = string.Empty;
}

/// <summary>DTO di creazione categoria.</summary>
public class CategoriaCreateDTO
{
    /// <summary>Nome della nuova categoria.</summary>
    public string Nome { get; set; } = string.Empty;
}

/// <summary>DTO di aggiornamento categoria.</summary>
public class CategoriaUpdateDTO
{
    /// <summary>Nome aggiornato della categoria.</summary>
    public string Nome { get; set; } = string.Empty;
}
