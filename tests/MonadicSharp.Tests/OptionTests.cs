using FluentAssertions;
using MonadicSharp;

namespace MonadicSharp.Tests;

public class OptionTests
{
    // ── Construction ────────────────────────────────────────────────────────

    [Fact]
    public void Some_creates_option_with_value()
    {
        var option = Option<int>.Some(42);
        option.HasValue.Should().BeTrue();
        option.IsNone.Should().BeFalse();
    }

    [Fact]
    public void None_creates_empty_option()
    {
        var option = Option<int>.None;
        option.IsNone.Should().BeTrue();
        option.HasValue.Should().BeFalse();
    }

    [Fact]
    public void Some_with_null_throws_ArgumentNullException()
    {
        var act = () => Option<string>.Some(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void From_null_returns_None()
    {
        Option<string>.From(null).IsNone.Should().BeTrue();
    }

    [Fact]
    public void From_non_null_returns_Some()
    {
        Option<string>.From("hello").HasValue.Should().BeTrue();
    }

    // ── Implicit conversion ──────────────────────────────────────────────────

    [Fact]
    public void Implicit_conversion_from_value_creates_Some()
    {
        Option<int> opt = 99;
        opt.HasValue.Should().BeTrue();
    }

    [Fact]
    public void Implicit_conversion_from_Option_None_creates_None()
    {
        Option<int> opt = Option.None;
        opt.IsNone.Should().BeTrue();
    }

    // ── Map ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Map_transforms_value_when_Some()
    {
        var result = Option<int>.Some(5).Map(x => x * 3);
        result.GetValueOrDefault(0).Should().Be(15);
    }

    [Fact]
    public void Map_returns_None_when_None()
    {
        var result = Option<int>.None.Map(x => x * 3);
        result.IsNone.Should().BeTrue();
    }

    // ── Bind ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Bind_chains_on_Some()
    {
        var result = Option<int>.Some(5)
            .Bind(x => Option<string>.Some($"val={x}"));
        result.GetValueOrDefault("none").Should().Be("val=5");
    }

    [Fact]
    public void Bind_short_circuits_on_None()
    {
        var called = false;
        var result = Option<int>.None
            .Bind(x => { called = true; return Option<string>.Some("ok"); });

        called.Should().BeFalse();
        result.IsNone.Should().BeTrue();
    }

    // ── Where ────────────────────────────────────────────────────────────────

    [Fact]
    public void Where_keeps_Some_when_predicate_passes()
    {
        Option<int>.Some(10).Where(x => x > 5).HasValue.Should().BeTrue();
    }

    [Fact]
    public void Where_returns_None_when_predicate_fails()
    {
        Option<int>.Some(3).Where(x => x > 5).IsNone.Should().BeTrue();
    }

    // ── Match ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Match_returns_onSome_result_when_Some()
    {
        var label = Option<int>.Some(7).Match(v => $"got {v}", () => "nothing");
        label.Should().Be("got 7");
    }

    [Fact]
    public void Match_returns_onNone_result_when_None()
    {
        var label = Option<int>.None.Match(v => "ok", () => "nothing");
        label.Should().Be("nothing");
    }

    // ── GetValueOrDefault ─────────────────────────────────────────────────────

    [Fact]
    public void GetValueOrDefault_returns_value_on_Some()
    {
        Option<int>.Some(5).GetValueOrDefault(0).Should().Be(5);
    }

    [Fact]
    public void GetValueOrDefault_returns_default_on_None()
    {
        Option<int>.None.GetValueOrDefault(99).Should().Be(99);
    }

    // ── Do ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Do_runs_action_on_Some()
    {
        var seen = 0;
        Option<int>.Some(3).Do(x => seen = x);
        seen.Should().Be(3);
    }

    [Fact]
    public void Do_does_not_run_on_None()
    {
        var called = false;
        Option<int>.None.Do(_ => called = true);
        called.Should().BeFalse();
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void Two_Some_with_same_value_are_equal()
    {
        (Option<int>.Some(5) == Option<int>.Some(5)).Should().BeTrue();
    }

    [Fact]
    public void Two_None_are_equal()
    {
        (Option<int>.None == Option<int>.None).Should().BeTrue();
    }

    [Fact]
    public void Some_and_None_are_not_equal()
    {
        (Option<int>.Some(1) != Option<int>.None).Should().BeTrue();
    }

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void Some_ToString_shows_value()
    {
        Option<int>.Some(42).ToString().Should().Be("Some(42)");
    }

    [Fact]
    public void None_ToString_returns_None()
    {
        Option<int>.None.ToString().Should().Be("None");
    }
}
