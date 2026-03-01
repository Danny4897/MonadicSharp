using FluentAssertions;
using MonadicSharp;

namespace MonadicSharp.Tests;

public class ErrorTests
{
    [Fact]
    public void Create_sets_message_and_default_code()
    {
        var error = Error.Create("something failed");
        error.Message.Should().Be("something failed");
        error.Code.Should().Be("GENERAL_ERROR");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void Create_with_custom_code_uses_that_code()
    {
        var error = Error.Create("msg", code: "MY_CODE");
        error.Code.Should().Be("MY_CODE");
    }

    [Fact]
    public void Validation_sets_correct_type_and_code()
    {
        var error = Error.Validation("Name is required", field: "Name");
        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().Be("VALIDATION_ERROR");
        error.Metadata["Field"].Should().Be("Name");
    }

    [Fact]
    public void NotFound_sets_correct_type_and_message()
    {
        var error = Error.NotFound("User", identifier: "42");
        error.Type.Should().Be(ErrorType.NotFound);
        error.Message.Should().Contain("User");
        error.Metadata["Identifier"].Should().Be("42");
    }

    [Fact]
    public void Forbidden_sets_correct_type()
    {
        var error = Error.Forbidden();
        error.Type.Should().Be(ErrorType.Forbidden);
        error.Code.Should().Be("FORBIDDEN");
    }

    [Fact]
    public void Conflict_sets_correct_type()
    {
        var error = Error.Conflict("already exists", resource: "Email");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Metadata["Resource"].Should().Be("Email");
    }

    [Fact]
    public void FromException_captures_exception_message_and_type()
    {
        var ex = new InvalidOperationException("bad state");
        var error = Error.FromException(ex);

        error.Message.Should().Be("bad state");
        error.Type.Should().Be(ErrorType.Exception);
        error.Metadata["ExceptionType"].Should().Be("InvalidOperationException");
    }

    [Fact]
    public void Combine_single_error_returns_same_error()
    {
        var error = Error.Create("one");
        Error.Combine(error).Should().Be(error);
    }

    [Fact]
    public void Combine_multiple_errors_creates_aggregate()
    {
        var combined = Error.Combine(
            Error.Create("first"),
            Error.Create("second"));

        combined.Code.Should().Be("MULTIPLE_ERRORS");
        combined.SubErrors.Should().HaveCount(2);
    }

    [Fact]
    public void Combine_with_no_errors_throws()
    {
        var act = () => Error.Combine();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithMetadata_adds_key_value_pair()
    {
        var error = Error.Create("msg").WithMetadata("requestId", "abc-123");
        error.Metadata["requestId"].Should().Be("abc-123");
    }

    [Fact]
    public void WithInnerError_sets_inner_error()
    {
        var inner = Error.Create("inner");
        var outer = Error.Create("outer").WithInnerError(inner);
        outer.InnerError.Should().Be(inner);
    }

    [Fact]
    public void IsOfType_returns_true_for_matching_type()
    {
        Error.Validation("v").IsOfType(ErrorType.Validation).Should().BeTrue();
    }

    [Fact]
    public void HasCode_is_case_insensitive()
    {
        Error.Create("msg", code: "MY_CODE").HasCode("my_code").Should().BeTrue();
    }

    [Fact]
    public void GetAllErrors_flattens_sub_errors()
    {
        var combined = Error.Combine(
            Error.Create("a"),
            Error.Create("b"),
            Error.Create("c"));

        combined.GetAllErrors().Should().HaveCount(4); // root + 3 sub
    }

    [Fact]
    public void ToJson_returns_valid_json_string()
    {
        var json = Error.Create("test error").ToJson();
        json.Should().Contain("message");
        json.Should().Contain("test error");
    }
}
