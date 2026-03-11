namespace CognomeNomeAPI.Model;

public class PriorityLog
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public double Score { get; set; }
    public string FactorsJson { get; set; } = null!;
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
