using FluentAssertions;
using MonadicSharp;

namespace MonadicSharp.Tests;

public class UnitTests
{
    [Fact]
    public void All_Unit_instances_are_equal()
    {
        var a = Unit.Value;
        var b = Unit.Value;
        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Unit_equals_any_Unit_object()
    {
        Unit.Value.Equals((object)Unit.Value).Should().BeTrue();
    }

    [Fact]
    public void Unit_GetHashCode_is_always_zero()
    {
        Unit.Value.GetHashCode().Should().Be(0);
    }

    [Fact]
    public void Unit_ToString_returns_parentheses()
    {
        Unit.Value.ToString().Should().Be("()");
    }

    [Fact]
    public void Unit_can_be_used_as_Result_value_type()
    {
        var result = Result<Unit>.Success(Unit.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
    }
}
