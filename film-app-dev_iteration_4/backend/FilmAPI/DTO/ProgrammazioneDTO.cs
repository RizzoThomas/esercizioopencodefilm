namespace FilmAPI.DTO;

public class ProgrammazioneFilmDTO
{
    public int Id { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public string? CopertinaPath { get; set; }
    public int Durata { get; set; }
    public List<CategoriaDTO> Categorie { get; set; } = new();
    public DateOnly? DataRilascio { get; set; }
    public bool InEvidenza { get; set; }
    public bool InUscita { get; set; }
    public int ShowCountNext7Days { get; set; }
    public bool DisponibileNelCinemaSelezionato { get; set; }
    public DateTime? ProssimoShowNelCinemaSelezionato { get; set; }
}

public class ProgrammazioneFilmPagedResultDTO
{
    public List<ProgrammazioneFilmDTO> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class FilmSchedaDTO
{
    public int Id { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public string? CopertinaPath { get; set; }
    public int Durata { get; set; }
    public DateTime DataProduzione { get; set; }
    public DateOnly? DataRilascio { get; set; }
    public string? DescrizioneLunga { get; set; }
    public string? CastText { get; set; }
    public List<string> CastList { get; set; } = new();
    public List<CategoriaDTO> Categorie { get; set; } = new();
    public string? RegistaNome { get; set; }
    public string? RegistaCognome { get; set; }
    public CinemaSintesiDTO? CinemaSelezionato { get; set; }
    public List<FilmSchedaShowGroupDTO> ShowCalendar { get; set; } = new();
}

public class FilmSchedaShowGroupDTO
{
    public DateOnly Data { get; set; }
    public List<FilmSchedaTipoSalaGroupDTO> GruppiPerTipoSala { get; set; } = new();
}

public class FilmSchedaTipoSalaGroupDTO
{
    public string TipoSala { get; set; } = string.Empty;
    public List<FilmSchedaShowItemDTO> Shows { get; set; } = new();
}

public class FilmSchedaShowItemDTO
{
    public int ShowId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public decimal PrezzoBase { get; set; }
    public decimal SupplementoSala { get; set; }
    public int SalaId { get; set; }
    public string? SalaNome { get; set; }
    public int SalaNumeroProgressivo { get; set; }
}

public class CinemaCardDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Citta { get; set; } = string.Empty;
    public string Indirizzo { get; set; } = string.Empty;
    public List<string> TipologieSalePresenti { get; set; } = new();
    public double? DistanzaKm { get; set; }
    public double? Latitudine { get; set; }
    public double? Longitudine { get; set; }
}

public class CinemaScheduleDayDTO
{
    public CinemaSintesiDTO Cinema { get; set; } = new();
    public DateOnly Data { get; set; }
    public List<CinemaScheduleFilmDTO> Films { get; set; } = new();
}

public class CinemaScheduleFilmDTO
{
    public int FilmId { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public string? CopertinaPath { get; set; }
    public string? DescrizioneEstratto { get; set; }
    public List<CinemaScheduleTipoSalaGroupDTO> GruppiPerTipoSala { get; set; } = new();
}

public class CinemaScheduleTipoSalaGroupDTO
{
    public string TipoSala { get; set; } = string.Empty;
    public List<CinemaScheduleShowItemDTO> Shows { get; set; } = new();
}

public class CinemaScheduleShowItemDTO
{
    public int ShowId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public int SalaId { get; set; }
    public string? SalaNome { get; set; }
    public int SalaNumeroProgressivo { get; set; }
}

public class CinemaSintesiDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Citta { get; set; } = string.Empty;
    public string Indirizzo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? CodiceLocale { get; set; }
}

public class CinemaPreferitoDTO
{
    public int? CinemaId { get; set; }
    public CinemaSintesiDTO? Cinema { get; set; }
}
