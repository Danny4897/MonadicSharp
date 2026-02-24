using FluentAssertions;
using MonadicSharp;

namespace MonadicSharp.Tests;

public class TryTests
{
    [Fact]
    public void Execute_returns_success_when_no_exception()
    {
        var result = Try.Execute(() => int.Parse("10"));
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10);
    }

    [Fact]
    public void Execute_returns_failure_when_exception_thrown()
    {
        var result = Try.Execute(() => int.Parse("not-a-number"));
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_returns_success_for_successful_task()
    {
        var result = await Try.ExecuteAsync(async () =>
        {
            await Task.Delay(1);
            return 42;
        });
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_returns_failure_when_task_throws()
    {
        var result = await Try.ExecuteAsync<int>(async () =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("async error");
        });
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("async error");
    }

    [Fact]
    public void Execute_with_input_returns_success()
    {
        var result = Try.Execute("123", input => int.Parse(input));
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(123);
    }

    [Fact]
    public void Execute_with_input_returns_failure_on_exception()
    {
        var result = Try.Execute("abc", input => int.Parse(input));
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_with_input_returns_success()
    {
        var result = await Try.ExecuteAsync("5", async input =>
        {
            await Task.Delay(1);
            return int.Parse(input) * 2;
        });
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10);
    }

    [Fact]
    public async Task ExecuteAsync_with_input_returns_failure_on_exception()
    {
        var result = await Try.ExecuteAsync<string, int>("bad", async input =>
        {
            await Task.Delay(1);
            return int.Parse(input);
        });
        result.IsFailure.Should().BeTrue();
    }
}
