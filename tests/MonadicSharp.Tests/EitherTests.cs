using FluentAssertions;
using MonadicSharp;

namespace MonadicSharp.Tests;

public class EitherTests
{
    [Fact]
    public void FromLeft_creates_left_either()
    {
        var either = Either<string, int>.FromLeft("error");
        either.IsLeft.Should().BeTrue();
        either.IsRight.Should().BeFalse();
    }

    [Fact]
    public void FromRight_creates_right_either()
    {
        var either = Either<string, int>.FromRight(42);
        either.IsRight.Should().BeTrue();
        either.IsLeft.Should().BeFalse();
    }

    [Fact]
    public void Left_property_returns_Some_when_left()
    {
        var either = Either<string, int>.FromLeft("err");
        either.Left.HasValue.Should().BeTrue();
        either.Left.GetValueOrDefault("").Should().Be("err");
    }

    [Fact]
    public void Right_property_returns_None_when_left()
    {
        var either = Either<string, int>.FromLeft("err");
        either.Right.IsNone.Should().BeTrue();
    }

    [Fact]
    public void Right_property_returns_Some_when_right()
    {
        var either = Either<string, int>.FromRight(10);
        either.Right.HasValue.Should().BeTrue();
        either.Right.GetValueOrDefault(0).Should().Be(10);
    }

    [Fact]
    public void Match_calls_onLeft_for_left_either()
    {
        var either = Either<string, int>.FromLeft("failure");
        var label = either.Match(l => $"left:{l}", r => $"right:{r}");
        label.Should().Be("left:failure");
    }

    [Fact]
    public void Match_calls_onRight_for_right_either()
    {
        var either = Either<string, int>.FromRight(7);
        var label = either.Match(l => "left", r => $"right:{r}");
        label.Should().Be("right:7");
    }

    [Fact]
    public void Map_transforms_right_value()
    {
        var either = Either<string, int>.FromRight(5).Map(x => x * 2);
        either.Right.GetValueOrDefault(0).Should().Be(10);
    }

    [Fact]
    public void Map_preserves_left_value()
    {
        var either = Either<string, int>.FromLeft("err").Map(x => x * 2);
        either.IsLeft.Should().BeTrue();
        either.Left.GetValueOrDefault("").Should().Be("err");
    }

    [Fact]
    public void Bind_chains_on_right()
    {
        var either = Either<string, int>.FromRight(4)
            .Bind(x => Either<string, string>.FromRight($"x={x}"));
        either.Right.GetValueOrDefault("").Should().Be("x=4");
    }

    [Fact]
    public void Bind_short_circuits_on_left()
    {
        var called = false;
        var either = Either<string, int>.FromLeft("stop")
            .Bind(x => { called = true; return Either<string, string>.FromRight("ok"); });

        called.Should().BeFalse();
        either.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void ToString_shows_Left_or_Right_with_value()
    {
        Either<string, int>.FromLeft("err").ToString().Should().Be("Left(err)");
        Either<string, int>.FromRight(42).ToString().Should().Be("Right(42)");
    }
}
