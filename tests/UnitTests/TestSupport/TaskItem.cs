using System;

namespace TestSupport;

// Minimal test-only copy of the production TaskItem model
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime? DueDate { get; set; }
    public int? ParentTaskId { get; set; }
}
