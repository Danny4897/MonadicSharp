using FluentAssertions;
using MonadicSharp.Query.Extensions;

namespace MonadicSharp.Query.Tests;

public class PartitionExtensionsTests
{
    [Fact]
    public void SuccessValues_ReturnsOnlySuccessfulValues()
    {
        var results = new[]
        {
            Result<int>.Success(1),
            Result<int>.Failure("err"),
            Result<int>.Success(3)
        };

        results.SuccessValues().Should().BeEquivalentTo([1, 3]);
    }

    [Fact]
    public void FailureErrors_ReturnsOnlyFailureErrors()
    {
        var results = new[]
        {
            Result<int>.Success(1),
            Result<int>.Failure(Error.Create("e1")),
            Result<int>.Failure(Error.Create("e2"))
        };

        results.FailureErrors().Should().HaveCount(2);
    }

    [Fact]
    public void SuccessRate_ReturnsCorrectRatio()
    {
        var results = new[]
        {
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.Failure("err")
        };

        results.SuccessRate().Should().BeApproximately(2.0 / 3.0, 0.001);
    }

    [Fact]
    public void SuccessRate_EmptySequence_ReturnsZero()
    {
        Array.Empty<Result<int>>().SuccessRate().Should().Be(0d);
    }

    [Fact]
    public void RequireSuccessRate_MeetsThreshold_ReturnsSuccess()
    {
        var results = Enumerable.Range(1, 8)
            .Select(i => Result<int>.Success(i))
            .Concat([Result<int>.Failure("err1"), Result<int>.Failure("err2")]);

        var outcome = results.RequireSuccessRate(0.7);
        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Should().HaveCount(8);
    }

    [Fact]
    public void RequireSuccessRate_BelowThreshold_ReturnsFailure()
    {
        var results = new[]
        {
            Result<int>.Success(1),
            Result<int>.Failure("e1"),
            Result<int>.Failure("e2"),
            Result<int>.Failure("e3")
        };

        var outcome = results.RequireSuccessRate(0.9);
        outcome.IsFailure.Should().BeTrue();
    }
}

public class ReconcileExtensionsTests
{
    [Fact]
    public void BestOf_ReturnsHighestScoringResult()
    {
        var results = new[]
        {
            Result<string>.Success("low"),
            Result<string>.Success("medium"),
            Result<string>.Success("high")
        };

        var best = results.BestOf(s => s.Length);
        best.IsSuccess.Should().BeTrue();
        best.Value.Should().Be("medium");
    }

    [Fact]
    public void BestOf_AllFailures_ReturnsFailure()
    {
        var results = new[]
        {
            Result<string>.Failure("e1"),
            Result<string>.Failure("e2")
        };

        results.BestOf(s => s.Length).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ReconcileBy_MergesMatchingKeys()
    {
        var left = new[]
        {
            Result<(int Id, string Name)>.Success((1, "Alice")),
            Result<(int Id, string Name)>.Success((2, "Bob"))
        };
        var right = new[]
        {
            Result<(int Id, int Score)>.Success((1, 95)),
            Result<(int Id, int Score)>.Success((3, 80))
        };

        var reconciled = left.ReconcileBy(
            right,
            leftKey:   l => l.Id,
            rightKey:  r => r.Id,
            leftOnly:  l => $"{l.Name}:no-score",
            rightOnly: r => $"unknown:{r.Score}",
            merge:     (l, r) => $"{l.Name}:{r.Score}").ToList();

        reconciled.Should().HaveCount(3);
        reconciled.Where(r => r.IsSuccess).Select(r => r.Value)
            .Should().Contain("Alice:95");
    }
}
