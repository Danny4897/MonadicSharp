using FluentAssertions;
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicSharp.Tests;

public class FunctionalExtensionsTests
{
    // ── ResultExtensions ─────────────────────────────────────────────────────

    [Fact]
    public void Sequence_returns_success_when_all_succeed()
    {
        var results = new[] { Result<int>.Success(1), Result<int>.Success(2), Result<int>.Success(3) };
        var combined = results.Sequence();
        combined.IsSuccess.Should().BeTrue();
        combined.Value.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void Sequence_fails_and_combines_errors_when_any_fail()
    {
        var results = new[]
        {
            Result<int>.Success(1),
            Result<int>.Failure("bad1"),
            Result<int>.Failure("bad2")
        };
        var combined = results.Sequence();
        combined.IsFailure.Should().BeTrue();
        combined.Error.SubErrors.Should().HaveCount(2);
    }

    [Fact]
    public void Traverse_applies_func_to_all_and_combines()
    {
        var numbers = new[] { "1", "2", "3" };
        var result = numbers.Traverse(s =>
            int.TryParse(s, out var n) ? Result<int>.Success(n) : Result<int>.Failure("parse error"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void Ensure_fails_when_predicate_is_false()
    {
        var result = Result<int>.Success(3)
            .Ensure(x => x > 10, "must be > 10");
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("must be > 10");
    }

    [Fact]
    public void Ensure_passes_when_predicate_is_true()
    {
        var result = Result<int>.Success(15)
            .Ensure(x => x > 10, "must be > 10");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Combine_two_results_succeeds_when_both_succeed()
    {
        var result = Result<int>.Success(2)
            .Combine(Result<string>.Success("hello"), (n, s) => $"{s}-{n}");
        result.Value.Should().Be("hello-2");
    }

    [Fact]
    public void Combine_two_results_aggregates_both_errors()
    {
        var result = Result<int>.Failure("err1")
            .Combine(Result<string>.Failure("err2"), (n, s) => "");
        result.IsFailure.Should().BeTrue();
        result.Error.SubErrors.Should().HaveCount(2);
    }

    [Fact]
    public async Task MapAsync_transforms_value_asynchronously()
    {
        var result = await Result<int>.Success(5)
            .MapAsync(async x => { await Task.Delay(1); return x * 2; });
        result.Value.Should().Be(10);
    }

    [Fact]
    public async Task BindAsync_chains_async_operation()
    {
        var result = await Result<int>.Success(5)
            .BindAsync(async x =>
            {
                await Task.Delay(1);
                return Result<string>.Success($"val={x}");
            });
        result.Value.Should().Be("val=5");
    }

    [Fact]
    public void OrElse_returns_original_on_success()
    {
        var result = Result<int>.Success(1).OrElse(Result<int>.Success(99));
        result.Value.Should().Be(1);
    }

    [Fact]
    public void OrElse_returns_alternative_on_failure()
    {
        var result = Result<int>.Failure("err").OrElse(Result<int>.Success(99));
        result.Value.Should().Be(99);
    }

    [Fact]
    public void OrElse_with_factory_calls_recovery_on_failure()
    {
        var result = Result<int>.Failure(Error.Create("err"))
            .OrElse(e => Result<int>.Success(42));
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task AsTask_wraps_result_in_task()
    {
        var task = Result<int>.Success(5).AsTask();
        var result = await task;
        result.Value.Should().Be(5);
    }

    // ── OptionExtensions ─────────────────────────────────────────────────────

    [Fact]
    public void Filter_keeps_Some_when_predicate_passes()
    {
        Option<int>.Some(10).Filter(x => x > 5).HasValue.Should().BeTrue();
    }

    [Fact]
    public void Filter_returns_None_when_predicate_fails()
    {
        Option<int>.Some(3).Filter(x => x > 5).IsNone.Should().BeTrue();
    }

    [Fact]
    public void Option_Sequence_returns_Some_when_all_have_value()
    {
        var options = new[] { Option<int>.Some(1), Option<int>.Some(2) };
        options.Sequence().HasValue.Should().BeTrue();
    }

    [Fact]
    public void Option_Sequence_returns_None_when_any_is_None()
    {
        var options = new[] { Option<int>.Some(1), Option<int>.None };
        options.Sequence().IsNone.Should().BeTrue();
    }

    [Fact]
    public void FirstOrNone_returns_first_Some()
    {
        var options = new[] { Option<int>.None, Option<int>.Some(5), Option<int>.Some(10) };
        options.FirstOrNone().GetValueOrDefault(0).Should().Be(5);
    }

    [Fact]
    public void FirstOrNone_returns_None_when_all_are_None()
    {
        var options = new[] { Option<int>.None, Option<int>.None };
        options.FirstOrNone().IsNone.Should().BeTrue();
    }

    [Fact]
    public void ToResult_converts_Some_to_success()
    {
        Option<int>.Some(5).ToResult("not found").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ToResult_converts_None_to_failure_with_error()
    {
        var result = Option<int>.None.ToResult("not found");
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("not found");
    }

    [Fact]
    public async Task Option_MapAsync_transforms_when_Some()
    {
        var result = await Option<int>.Some(3)
            .MapAsync(async x => { await Task.Delay(1); return x * 4; });
        result.GetValueOrDefault(0).Should().Be(12);
    }

    [Fact]
    public async Task Option_AsTask_wraps_option()
    {
        var option = await Option<int>.Some(7).AsTask();
        option.HasValue.Should().BeTrue();
    }

    // ── TaskResultExtensions ──────────────────────────────────────────────────

    [Fact]
    public async Task TaskResult_Map_transforms_value()
    {
        var result = await Task.FromResult(Result<int>.Success(4))
            .Map(x => x + 1);
        result.Value.Should().Be(5);
    }

    [Fact]
    public async Task TaskResult_Bind_chains_async_operation()
    {
        var result = await Task.FromResult(Result<int>.Success(3))
            .Bind(async x =>
            {
                await Task.Delay(1);
                return Result<string>.Success($"n={x}");
            });
        result.Value.Should().Be("n=3");
    }

    [Fact]
    public async Task TaskResult_Do_executes_side_effect_on_success()
    {
        var seen = 0;
        await Task.FromResult(Result<int>.Success(9))
            .Do(async x => { seen = x; await Task.CompletedTask; });
        seen.Should().Be(9);
    }
}
