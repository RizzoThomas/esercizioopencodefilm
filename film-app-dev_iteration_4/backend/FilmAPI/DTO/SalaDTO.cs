using FilmAPI.Model;

namespace FilmAPI.DTO;

public class SalaDTO
{
    public int Id { get; set; }
    public int CinemaId { get; set; }
    public int NumeroProgressivo { get; set; }
    public TipoSala TipoSala { get; set; }
    public string? Nome { get; set; }
    public decimal Supplemento { get; set; }
    public bool IsAttiva { get; set; }
    public List<SalaPostoDTO> Posti { get; set; } = new();
}

public class SalaCreateDTO
{
    public int CinemaId { get; set; }
    public int NumeroProgressivo { get; set; }
    public TipoSala TipoSala { get; set; }
    public string? Nome { get; set; }
    public decimal Supplemento { get; set; }
    public bool IsAttiva { get; set; } = true;
}

public class SalaUpdateDTO
{
    public TipoSala TipoSala { get; set; }
    public string? Nome { get; set; }
    public decimal Supplemento { get; set; }
    public bool IsAttiva { get; set; }
}

public class SalaPostoDTO
{
    public int Id { get; set; }
    public int SalaId { get; set; }
    public string Settore { get; set; } = "PLATEA";
    public int Fila { get; set; }
    public int Numero { get; set; }
    public int? PosX { get; set; }
    public int? PosY { get; set; }
    public bool IsWheelchair { get; set; }
    public bool IsAttivo { get; set; } = true;
}

public class SalaLayoutSaveDTO
{
    public List<SalaPostoDTO> Posti { get; set; } = new();
}
