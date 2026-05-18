using FilmAPI.Model;

namespace FilmAPI.DTO;

/// <summary>DTO di una sala usato nelle API di gestione cinema.</summary>
public class SalaDTO
{
    /// <summary>ID univoco della sala.</summary>
    public int Id { get; set; }
    /// <summary>ID del cinema proprietario della sala.</summary>
    public int CinemaId { get; set; }
    /// <summary>Numero progressivo della sala all'interno del cinema.</summary>
    public int NumeroProgressivo { get; set; }
    /// <summary>Tipo sala; serve a descrivere configurazione e prezzo.</summary>
    public TipoSala TipoSala { get; set; }
    /// <summary>Nome opzionale della sala; utile per identificazione umana.</summary>
    public string? Nome { get; set; }
    /// <summary>Supplemento applicato alla sala; serve per il pricing.</summary>
    public decimal Supplemento { get; set; }
    /// <summary>Indica se la sala è attiva.</summary>
    public bool IsAttiva { get; set; }
    /// <summary>Posti configurati nella sala; servono per la mappa posti.</summary>
    public List<SalaPostoDTO> Posti { get; set; } = new();
}

public class SalaCreateDTO
{
    /// <summary>ID del cinema in cui creare la sala.</summary>
    public int CinemaId { get; set; }
    /// <summary>Numero progressivo della sala.</summary>
    public int NumeroProgressivo { get; set; }
    /// <summary>Tipo della sala da creare.</summary>
    public TipoSala TipoSala { get; set; }
    /// <summary>Nome opzionale della sala.</summary>
    public string? Nome { get; set; }
    /// <summary>Supplemento da applicare alla sala.</summary>
    public decimal Supplemento { get; set; }
    /// <summary>Indica se la sala è attiva al momento della creazione.</summary>
    public bool IsAttiva { get; set; } = true;
}

public class SalaUpdateDTO
{
    /// <summary>Nuovo tipo sala.</summary>
    public TipoSala TipoSala { get; set; }
    /// <summary>Nuovo nome opzionale della sala.</summary>
    public string? Nome { get; set; }
    /// <summary>Nuovo supplemento sala.</summary>
    public decimal Supplemento { get; set; }
    /// <summary>Stato attivo aggiornato.</summary>
    public bool IsAttiva { get; set; }
}

public class SalaPostoDTO
{
    /// <summary>ID univoco del posto.</summary>
    public int Id { get; set; }
    /// <summary>ID della sala a cui appartiene il posto.</summary>
    public int SalaId { get; set; }
    /// <summary>Settore del posto; serve a distinguere le aree in sala.</summary>
    public string Settore { get; set; } = "PLATEA";
    /// <summary>Fila del posto; serve alla disposizione fisica.</summary>
    public int Fila { get; set; }
    /// <summary>Numero del posto nella fila.</summary>
    public int Numero { get; set; }
    /// <summary>Coordinata X opzionale per il layout grafico.</summary>
    public int? PosX { get; set; }
    /// <summary>Coordinata Y opzionale per il layout grafico.</summary>
    public int? PosY { get; set; }
    /// <summary>Indica se il posto è accessibile in sedia a rotelle.</summary>
    public bool IsWheelchair { get; set; }
    /// <summary>Indica se il posto è attivo e prenotabile.</summary>
    public bool IsAttivo { get; set; } = true;
}

public class SalaLayoutSaveDTO
{
    /// <summary>Lista completa dei posti da salvare.</summary>
    public List<SalaPostoDTO> Posti { get; set; } = new();
}
