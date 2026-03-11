namespace CognomeNomeAPI.Model;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int? AssigneeId { get; set; }
    public int? CreatorId { get; set; }
    public int? TeamId { get; set; }
    public double PriorityScore { get; set; }
    public string Status { get; set; } = "Todo";
    public DateTime? DueDate { get; set; }
    public int? ParentTaskId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
