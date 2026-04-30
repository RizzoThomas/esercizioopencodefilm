namespace FilmAPI.DTO;

using System.Text.Json.Serialization;

public class SegnalazioneDTO
{
    public int Id { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public string Descrizione { get; set; } = string.Empty;
    public string? EmailUtente { get; set; }
    public int? UserId { get; set; }
    public string Stato { get; set; } = "Aperta";
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateSegnalazioneDTO
{
    public string Titolo { get; set; } = string.Empty;
    public string Descrizione { get; set; } = string.Empty;
    public string? EmailUtente { get; set; }
    public int? UserId { get; set; }
}

public class SegnalazioniStore
{
    private static readonly object _lock = new();
    private static List<SegnalazioneDTO>? _segnalazioni;
    private static int _nextId = 1;
    private static readonly string _filePath = Path.Combine(AppContext.BaseDirectory, "Data", "segnalazioni.json");

    public static List<SegnalazioneDTO> GetAll()
    {
        LoadFromFile();
        return _segnalazioni!;
    }

    public static SegnalazioneDTO Add(CreateSegnalazioneDTO dto)
    {
        LoadFromFile();
        var s = new SegnalazioneDTO
        {
            Id = _nextId++,
            Titolo = dto.Titolo,
            Descrizione = dto.Descrizione,
            EmailUtente = dto.EmailUtente,
            UserId = dto.UserId,
            Stato = "Aperta",
            CreatedAtUtc = DateTime.UtcNow
        };
        _segnalazioni!.Add(s);
        SaveToFile();
        return s;
    }

    public static SegnalazioneDTO? UpdateStato(int id, string stato)
    {
        LoadFromFile();
        var s = _segnalazioni!.FirstOrDefault(x => x.Id == id);
        if (s == null) return null;
        s.Stato = stato;
        SaveToFile();
        return s;
    }

    private static void LoadFromFile()
    {
        if (_segnalazioni != null) return;
        lock (_lock)
        {
            if (_segnalazioni != null) return;
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _segnalazioni = System.Text.Json.JsonSerializer.Deserialize<List<SegnalazioneDTO>>(json) ?? new();
                    _nextId = _segnalazioni.Any() ? _segnalazioni.Max(x => x.Id) + 1 : 1;
                }
                else
                {
                    _segnalazioni = new List<SegnalazioneDTO>();
                }
            }
            catch
            {
                _segnalazioni = new List<SegnalazioneDTO>();
            }
        }
    }

    private static void SaveToFile()
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
                var json = System.Text.Json.JsonSerializer.Serialize(_segnalazioni, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch { }
        }
    }
}
