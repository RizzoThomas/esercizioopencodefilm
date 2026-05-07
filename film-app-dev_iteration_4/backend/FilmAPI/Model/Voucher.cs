namespace FilmAPI.Model;

public class Voucher
{
    public int Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public decimal ImportoIniziale { get; set; }
    public decimal SaldoResiduo { get; set; }
    public string Stato { get; set; } = "attivo";
    public DateTime? DataScadenza { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
