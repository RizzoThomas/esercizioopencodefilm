using CognomeNomeAPI.Model;

namespace CognomeNomeAPI.Services.Scoring;

public interface IScoringService
{
    // Compute a priority score and return score + JSON-serializable factors
    (double score, string factorsJson) ComputePriority(TaskItem task, Func<int?, int> dependencyDepthProvider);
}
