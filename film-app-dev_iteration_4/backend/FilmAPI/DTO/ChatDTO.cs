namespace FilmAPI.DTO;

public class ChatRequestDTO
{
    public string Message { get; set; } = string.Empty;
    public int FailedAttempts { get; set; } = 0;
}

public class ChatResponseDTO
{
    public string Reply { get; set; } = string.Empty;
    public bool IsResolved { get; set; } = true;
    public bool ShowTicketButton { get; set; } = false;
}

public class CreateTicketDTO
{
    public string Oggetto { get; set; } = string.Empty;
    public string Messaggio { get; set; } = string.Empty;
    public string? EmailContatto { get; set; }
}

public class SupportTicketDTO
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? NomeUtente { get; set; }
    public string? EmailUtente { get; set; }
    public string Oggetto { get; set; } = string.Empty;
    public string Messaggio { get; set; } = string.Empty;
    public string? EmailContatto { get; set; }
    public string Stato { get; set; } = string.Empty;
    public DateTime CreatoIl { get; set; }
    public DateTime? RisoltoIl { get; set; }
}
