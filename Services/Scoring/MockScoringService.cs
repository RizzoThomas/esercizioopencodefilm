using System.Text.Json;
using CognomeNomeAPI.Model;

namespace CognomeNomeAPI.Services.Scoring;

public class MockScoringService : IScoringService
{
    // Very simple heuristic: base on presence of due date and approximate urgency
    public (double score, string factorsJson) ComputePriority(TaskItem task, Func<int?, int> dependencyDepthProvider)
    {
        var urgency = 0.0;
        if (task.DueDate.HasValue)
        {
            var days = (task.DueDate.Value - DateTime.UtcNow).TotalDays;
            urgency = Math.Max(0, 1.0 / (1 + Math.Max(days, 0)));
        }

        var impact = 1.0; // placeholder: could be derived from team/assignee
        var depth = dependencyDepthProvider(task.ParentTaskId);
        var depthFactor = 1.0 / (1 + depth);

        var score = Math.Round((0.6 * urgency + 0.3 * impact + 0.1 * depthFactor) * 100, 2);

        var factors = new { urgency, impact, depth, depthFactor };
        var json = JsonSerializer.Serialize(factors);
        return (score, json);
    }
}
