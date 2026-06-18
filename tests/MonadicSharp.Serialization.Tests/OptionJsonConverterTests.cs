using System.Text.Json;
using FluentAssertions;
using MonadicSharp.Serialization;

namespace MonadicSharp.Serialization.Tests;

public class OptionJsonConverterTests
{
    private readonly JsonSerializerOptions _opts = SerializationExtensions.CreateDefaultOptions();

    [Fact]
    public void Serialize_SomeOption_ProducesExpectedJson()
    {
        var option = Option<string>.Some("value");
        var json = JsonSerializer.Serialize(option, _opts);

        json.Should().Contain("\"hasValue\":true");
        json.Should().Contain("\"value\":\"value\"");
    }

    [Fact]
    public void Serialize_NoneOption_ProducesExpectedJson()
    {
        var option = Option<string>.None;
        var json = JsonSerializer.Serialize(option, _opts);

        json.Should().Contain("\"hasValue\":false");
        json.Should().NotContain("\"value\"");
    }

    [Fact]
    public void Deserialize_SomeJson_ReturnsSomeOption()
    {
        const string json = """{"hasValue":true,"value":99}""";
        var option = JsonSerializer.Deserialize<Option<int>>(json, _opts);

        option.HasValue.Should().BeTrue();
        option.GetValueOrDefault(0).Should().Be(99);
    }

    [Fact]
    public void Deserialize_NoneJson_ReturnsNoneOption()
    {
        const string json = """{"hasValue":false}""";
        var option = JsonSerializer.Deserialize<Option<int>>(json, _opts);

        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_SomeOption_IsIdempotent()
    {
        var original = Option<double>.Some(3.14);
        var json = JsonSerializer.Serialize(original, _opts);
        var restored = JsonSerializer.Deserialize<Option<double>>(json, _opts);

        restored.HasValue.Should().BeTrue();
        restored.GetValueOrDefault(0.0).Should().BeApproximately(3.14, 0.001);
    }
}
