using System;
using TestSupport;
using Xunit;

namespace UnitTests;

public class ScoringTests
{
    [Fact]
    public void ComputePriority_NoDueDate_ReturnsNonZeroScore()
    {
        var svc = new MockScoringService();
        var task = new TaskItem { Id = 1, Title = "T", DueDate = null };
        var (score, json) = svc.ComputePriority(task, parentId => 0);
        Assert.True(score >= 0);
        Assert.False(string.IsNullOrEmpty(json));
    }

    [Fact]
    public void ComputePriority_CloseDueDate_HigherUrgency()
    {
        var svc = new MockScoringService();
        var soon = new TaskItem { Id = 1, Title = "Soon", DueDate = DateTime.UtcNow.AddDays(1) };
        var later = new TaskItem { Id = 2, Title = "Later", DueDate = DateTime.UtcNow.AddDays(30) };
        var (s1, _) = svc.ComputePriority(soon, parentId => 0);
        var (s2, _) = svc.ComputePriority(later, parentId => 0);
        Assert.True(s1 > s2);
    }

    [Fact]
    public void ComputePriority_DepthReducesScore()
    {
        var svc = new MockScoringService();
        var baseTask = new TaskItem { Id = 1, Title = "Base", DueDate = DateTime.UtcNow.AddDays(10) };
        var (s0, _) = svc.ComputePriority(baseTask, parentId => 0);
        var (s1, _) = svc.ComputePriority(baseTask, parentId => 2);
        Assert.True(s0 > s1);
    }
}
