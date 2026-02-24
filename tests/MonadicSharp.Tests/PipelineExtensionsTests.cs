using FluentAssertions;
using MonadicSharp;
using MonadicSharp.Extensions;

namespace MonadicSharp.Tests;

public class PipelineExtensionsTests
{
    // ── PipeExtensions ────────────────────────────────────────────────────────

    [Fact]
    public void Pipe_applies_function_to_value()
    {
        var result = 5.Pipe(x => x * 2);
        result.Should().Be(10);
    }

    [Fact]
    public async Task PipeAsync_applies_async_function()
    {
        var result = await 5.PipeAsync(async x => { await Task.Delay(1); return x * 3; });
        result.Should().Be(15);
    }

    [Fact]
    public async Task Task_Pipe_applies_function_after_awaiting()
    {
        var result = await Task.FromResult(5).Pipe(x => x + 10);
        result.Should().Be(15);
    }

    // ── PipelineExtensions ────────────────────────────────────────────────────

    [Fact]
    public async Task PipelineAsync_executes_all_steps_on_success()
    {
        var result = await Task.FromResult(Result<int>.Success(1))
            .PipelineAsync(
                async x => { await Task.Delay(1); return Result<int>.Success(x + 1); },
                async x => { await Task.Delay(1); return Result<int>.Success(x * 3); }
            );

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(6); // (1+1)*3
    }

    [Fact]
    public async Task PipelineAsync_stops_at_first_failure()
    {
        var step2Called = false;

        var result = await Task.FromResult(Result<int>.Success(1))
            .PipelineAsync(
                async x => { await Task.Delay(1); return Result<int>.Failure("stop here"); },
                async x => { step2Called = true; await Task.Delay(1); return Result<int>.Success(x); }
            );

        step2Called.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("stop here");
    }

    // ── ThenIf ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ThenIf_executes_operation_when_condition_true()
    {
        var result = await Task.FromResult(Result<int>.Success(10))
            .ThenIf(x => x > 5, async x => { await Task.Delay(1); return Result<int>.Success(x * 2); });

        result.Value.Should().Be(20);
    }

    [Fact]
    public async Task ThenIf_skips_operation_when_condition_false()
    {
        var called = false;
        var result = await Task.FromResult(Result<int>.Success(3))
            .ThenIf(x => x > 5, async x => { called = true; await Task.Delay(1); return Result<int>.Success(x); });

        called.Should().BeFalse();
        result.Value.Should().Be(3);
    }

    // ── ThenWithRetry ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ThenWithRetry_succeeds_on_first_attempt()
    {
        var attempts = 0;
        var result = await Task.FromResult(Result<int>.Success(1))
            .ThenWithRetry(async x =>
            {
                attempts++;
                await Task.Delay(1);
                return Result<int>.Success(x + 10);
            }, maxAttempts: 3, delay: TimeSpan.FromMilliseconds(1));

        attempts.Should().Be(1);
        result.Value.Should().Be(11);
    }

    [Fact]
    public async Task ThenWithRetry_retries_on_failure_and_exhausts()
    {
        var attempts = 0;
        var result = await Task.FromResult(Result<int>.Success(1))
            .ThenWithRetry(async x =>
            {
                attempts++;
                await Task.Delay(1);
                return Result<int>.Failure("always fails");
            }, maxAttempts: 3, delay: TimeSpan.FromMilliseconds(1));

        attempts.Should().Be(3);
        result.IsFailure.Should().BeTrue();
    }

    // ── PipelineBuilder ───────────────────────────────────────────────────────

    [Fact]
    public async Task PipelineBuilder_executes_chained_steps()
    {
        var result = await Task.FromResult(Result<int>.Success(1))
            .Then(async x => { await Task.Delay(1); return Result<int>.Success(x + 1); })
            .Then(async x => { await Task.Delay(1); return Result<int>.Success(x * 5); })
            .ExecuteAsync();

        result.Value.Should().Be(10); // (1+1)*5
    }

    [Fact]
    public async Task PipelineBuilder_ThenIf_skips_when_condition_false()
    {
        var called = false;
        var result = await Task.FromResult(Result<int>.Success(2))
            .Then(async x => { await Task.Delay(1); return Result<int>.Success(x); })
            .ThenIf(x => x > 100, async x => { called = true; return Result<int>.Success(x); })
            .ExecuteAsync();

        called.Should().BeFalse();
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task PipelineBuilder_Do_runs_side_effect_without_changing_value()
    {
        var log = new List<int>();

        var result = await Task.FromResult(Result<int>.Success(7))
            .Then(async x => { await Task.Delay(1); return Result<int>.Success(x); })
            .Do(async x => { log.Add(x); await Task.CompletedTask; })
            .ExecuteAsync();

        log.Should().Contain(7);
        result.Value.Should().Be(7);
    }
}
