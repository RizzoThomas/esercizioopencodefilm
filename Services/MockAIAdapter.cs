using System.Text.RegularExpressions;

namespace CognomeNomeAPI.Services;

public class MockAIAdapter : IAIAdapter
{
    public Task<(string Title, string? Description, DateTime? DueDate)> ParseTaskAsync(string text)
    {
        // Very naive parse: title = first sentence, look for 'by <date>' patterns
        var title = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? text;
        DateTime? due = null;
        var m = Regex.Match(text, @"by\s+(\d{4}-\d{2}-\d{2})");
        if (m.Success && DateTime.TryParse(m.Groups[1].Value, out var d)) due = d;
        return Task.FromResult((Title: title.Length > 100 ? title.Substring(0, 100) : title, Description: text, DueDate: due));
    }
}
