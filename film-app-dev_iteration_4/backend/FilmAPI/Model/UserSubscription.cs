namespace FilmAPI.Model;

public class UserSubscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int AbbonamentoId { get; set; }
    public Abbonamento Abbonamento { get; set; } = null!;
    public string MetodoPagamento { get; set; } = "carta";
    public bool AutoRinnovo { get; set; } = true;
    public DateTime DataInizio { get; set; }
    public DateTime DataScadenza { get; set; }
    public string Stato { get; set; } = "attivo";
    public DateTime CreatedAtUtc { get; set; }
}
