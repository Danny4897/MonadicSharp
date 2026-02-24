using FluentAssertions;
using MonadicSharp;

namespace MonadicSharp.Tests;

public class ResultTests
{
    // ── Construction ────────────────────────────────────────────────────────

    [Fact]
    public void Success_creates_successful_result()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_with_error_creates_failed_result()
    {
        var error = Error.Create("Something went wrong");
        var result = Result<int>.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Be("Something went wrong");
    }

    [Fact]
    public void Failure_with_message_creates_failed_result()
    {
        var result = Result<int>.Failure("bad input");

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("bad input");
    }

    [Fact]
    public void Success_with_null_throws_ArgumentNullException()
    {
        var act = () => Result<string>.Success(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Accessing_Value_on_failure_throws_InvalidOperationException()
    {
        var result = Result<int>.Failure("err");
        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Accessing_Error_on_success_throws_InvalidOperationException()
    {
        var result = Result<int>.Success(1);
        var act = () => result.Error;
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Implicit conversions ────────────────────────────────────────────────

    [Fact]
    public void Implicit_conversion_from_value_creates_success()
    {
        Result<string> result = "hello";
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void Implicit_conversion_from_Error_creates_failure()
    {
        Result<int> result = Error.Create("boom");
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Implicit_conversion_from_Exception_creates_failure()
    {
        Result<int> result = new InvalidOperationException("oops");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Exception);
    }

    // ── Map ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Map_transforms_value_on_success()
    {
        var result = Result<int>.Success(5).Map(x => x * 2);
        result.Value.Should().Be(10);
    }

    [Fact]
    public void Map_propagates_error_on_failure()
    {
        var error = Error.Create("fail");
        var result = Result<int>.Failure(error).Map(x => x * 2);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    // ── Bind ────────────────────────────────────────────────────────────────

    [Fact]
    public void Bind_chains_operations_on_success()
    {
        var result = Result<int>.Success(5)
            .Bind(x => Result<string>.Success($"value is {x}"));

        result.Value.Should().Be("value is 5");
    }

    [Fact]
    public void Bind_short_circuits_on_failure()
    {
        var called = false;
        var result = Result<int>.Failure("err")
            .Bind(x => { called = true; return Result<string>.Success("ok"); });

        called.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
    }

    // ── MapError ────────────────────────────────────────────────────────────

    [Fact]
    public void MapError_transforms_error_on_failure()
    {
        var result = Result<int>.Failure("original")
            .MapError(e => Error.Create("transformed"));

        result.Error.Message.Should().Be("transformed");
    }

    [Fact]
    public void MapError_does_not_run_on_success()
    {
        var called = false;
        var result = Result<int>.Success(1)
            .MapError(e => { called = true; return e; });

        called.Should().BeFalse();
        result.IsSuccess.Should().BeTrue();
    }

    // ── Do / DoError ────────────────────────────────────────────────────────

    [Fact]
    public void Do_runs_action_on_success_and_returns_same_result()
    {
        var seen = 0;
        var result = Result<int>.Success(7).Do(x => seen = x);

        seen.Should().Be(7);
        result.Value.Should().Be(7);
    }

    [Fact]
    public void DoError_runs_action_on_failure_and_returns_same_result()
    {
        string? seen = null;
        var result = Result<int>.Failure("err").DoError(e => seen = e.Message);

        seen.Should().Be("err");
        result.IsFailure.Should().BeTrue();
    }

    // ── Where ───────────────────────────────────────────────────────────────

    [Fact]
    public void Where_keeps_result_when_predicate_passes()
    {
        var result = Result<int>.Success(10).Where(x => x > 5, "too small");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Where_fails_result_when_predicate_fails()
    {
        var result = Result<int>.Success(3).Where(x => x > 5, "too small");
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("too small");
    }

    // ── Match ───────────────────────────────────────────────────────────────

    [Fact]
    public void Match_returns_onSuccess_result_when_success()
    {
        var label = Result<int>.Success(42).Match(v => $"got {v}", e => "error");
        label.Should().Be("got 42");
    }

    [Fact]
    public void Match_returns_onFailure_result_when_failure()
    {
        var label = Result<int>.Failure("boom").Match(v => "ok", e => e.Message);
        label.Should().Be("boom");
    }

    // ── GetValueOrDefault ────────────────────────────────────────────────────

    [Fact]
    public void GetValueOrDefault_returns_value_on_success()
    {
        Result<int>.Success(5).GetValueOrDefault(0).Should().Be(5);
    }

    [Fact]
    public void GetValueOrDefault_returns_default_on_failure()
    {
        Result<int>.Failure("err").GetValueOrDefault(99).Should().Be(99);
    }

    [Fact]
    public void GetValueOrDefault_factory_not_called_on_success()
    {
        var called = false;
        Result<int>.Success(5).GetValueOrDefault(() => { called = true; return 0; });
        called.Should().BeFalse();
    }

    // ── ToOption ────────────────────────────────────────────────────────────

    [Fact]
    public void ToOption_returns_Some_on_success()
    {
        Result<int>.Success(3).ToOption().HasValue.Should().BeTrue();
    }

    [Fact]
    public void ToOption_returns_None_on_failure()
    {
        Result<int>.Failure("err").ToOption().IsNone.Should().BeTrue();
    }

    // ── Combine (static) ────────────────────────────────────────────────────

    [Fact]
    public void Combine_returns_all_values_when_all_succeed()
    {
        var combined = Result.Combine(
            Result<int>.Success(1),
            Result<int>.Success(2),
            Result<int>.Success(3));

        combined.IsSuccess.Should().BeTrue();
        combined.Value.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void Combine_fails_when_any_result_fails()
    {
        var combined = Result.Combine(
            Result<int>.Success(1),
            Result<int>.Failure("bad"),
            Result<int>.Success(3));

        combined.IsFailure.Should().BeTrue();
    }

    // ── Try (static) ────────────────────────────────────────────────────────

    [Fact]
    public void Try_wraps_successful_func_in_Result()
    {
        var result = Result.Try(() => int.Parse("42"));
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Try_wraps_exception_in_Failure()
    {
        var result = Result.Try(() => int.Parse("not-a-number"));
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Exception);
    }

    [Fact]
    public async Task TryAsync_wraps_successful_async_func()
    {
        var result = await Result.TryAsync(async () =>
        {
            await Task.Delay(1);
            return 99;
        });
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(99);
    }

    [Fact]
    public async Task TryAsync_wraps_async_exception_in_Failure()
    {
        var result = await Result.TryAsync<int>(async () =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("async boom");
        });
        result.IsFailure.Should().BeTrue();
    }

    // ── Equality ────────────────────────────────────────────────────────────

    [Fact]
    public void Two_successes_with_same_value_are_equal()
    {
        var a = Result<int>.Success(5);
        var b = Result<int>.Success(5);
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Two_failures_with_same_error_are_equal()
    {
        var error = Error.Create("err");
        var a = Result<int>.Failure(error);
        var b = Result<int>.Failure(error);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Success_and_failure_are_not_equal()
    {
        var a = Result<int>.Success(1);
        var b = Result<int>.Failure("err");
        (a != b).Should().BeTrue();
    }

    // ── ToString ─────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_shows_Success_with_value()
    {
        Result<int>.Success(42).ToString().Should().Be("Success(42)");
    }

    [Fact]
    public void ToString_shows_Failure_with_error()
    {
        Result<int>.Failure("bad").ToString().Should().StartWith("Failure(");
    }
}
