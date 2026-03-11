namespace CognomeNomeAPI.Services;

public interface IAIAdapter
{
    // Parse a natural language task into structured fields (title, due)
    Task<(string Title, string? Description, DateTime? DueDate)> ParseTaskAsync(string text);
}
