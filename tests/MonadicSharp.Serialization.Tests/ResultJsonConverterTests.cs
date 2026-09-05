using System.Text.Json;
using FluentAssertions;
using MonadicSharp.Serialization;

namespace MonadicSharp.Serialization.Tests;

public class ResultJsonConverterTests
{
    private readonly JsonSerializerOptions _opts = SerializationExtensions.CreateDefaultOptions();

    [Fact]
    public void Serialize_SuccessResult_ProducesExpectedJson()
    {
        var result = Result<string>.Success("hello");
        var json = JsonSerializer.Serialize(result, _opts);

        json.Should().Contain("\"ok\":true");
        json.Should().Contain("\"value\":\"hello\"");
    }

    [Fact]
    public void Serialize_FailureResult_ProducesExpectedJson()
    {
        var result = Result<string>.Failure(Error.Create("something went wrong", "ERR_01"));
        var json = JsonSerializer.Serialize(result, _opts);

        json.Should().Contain("\"ok\":false");
        json.Should().Contain("\"error\":\"something went wrong\"");
        json.Should().Contain("\"code\":\"ERR_01\"");
    }

    [Fact]
    public void Deserialize_SuccessJson_ReturnsSuccessResult()
    {
        const string json = """{"ok":true,"value":"world"}""";
        var result = JsonSerializer.Deserialize<Result<string>>(json, _opts);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("world");
    }

    [Fact]
    public void Deserialize_FailureJson_ReturnsFailureResult()
    {
        const string json = """{"ok":false,"error":"not found","code":"NOT_FOUND","type":"NotFound"}""";
        var result = JsonSerializer.Deserialize<Result<string>>(json, _opts);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("not found");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void RoundTrip_SuccessResult_IsIdempotent()
    {
        var original = Result<int>.Success(42);
        var json = JsonSerializer.Serialize(original, _opts);
        var restored = JsonSerializer.Deserialize<Result<int>>(json, _opts);

        restored.IsSuccess.Should().BeTrue();
        restored.Value.Should().Be(42);
    }

    [Fact]
    public void RoundTrip_FailureResult_PreservesErrorMessage()
    {
        var original = Result<int>.Failure(Error.Validation("invalid input", "amount"));
        var json = JsonSerializer.Serialize(original, _opts);
        var restored = JsonSerializer.Deserialize<Result<int>>(json, _opts);

        restored.IsFailure.Should().BeTrue();
        restored.Error.Message.Should().Be("invalid input");
    }

    [Fact]
    public void ToJson_ExtensionMethod_WorksOnSuccessResult()
    {
        var result = Result<bool>.Success(true);
        var json = result.ToJson(_opts);

        json.Should().Contain("\"ok\":true");
        json.Should().Contain("\"value\":true");
    }

    [Fact]
    public void FromJson_ExtensionMethod_WorksOnSuccessJson()
    {
        var result = SerializationExtensions.FromJson<bool>("""{"ok":true,"value":true}""", _opts);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }
}
