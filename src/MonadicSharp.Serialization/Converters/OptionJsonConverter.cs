#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonadicSharp.Serialization.Converters;

/// <summary>
/// STJ factory that produces <see cref="OptionJsonConverter{T}"/> for any <c>Option&lt;T&gt;</c>.
/// Register via <see cref="SerializationExtensions.AddMonadicSharp"/>.
/// </summary>
public sealed class OptionJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(Option<>);

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// Serializes <c>Option&lt;T&gt;</c> as:
/// <list type="bullet">
/// <item>Some: <c>{"hasValue":true,"value":&lt;T&gt;}</c></item>
/// <item>None: <c>{"hasValue":false}</c></item>
/// </list>
/// </summary>
public sealed class OptionJsonConverter<T> : JsonConverter<Option<T>>
{
    /// <inheritdoc/>
    public override Option<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("hasValue", out var hasValueProp))
            throw new JsonException("Expected 'hasValue' property in Option JSON.");

        if (!hasValueProp.GetBoolean())
            return Option<T>.None;

        if (!root.TryGetProperty("value", out var valueProp))
            throw new JsonException("Expected 'value' property in Some Option JSON.");

        var value = JsonSerializer.Deserialize<T>(valueProp.GetRawText(), options)
                    ?? throw new JsonException("Option value deserialized to null.");
        return Option<T>.Some(value);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Option<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.HasValue)
        {
            writer.WriteBoolean("hasValue", true);
            writer.WritePropertyName("value");
            JsonSerializer.Serialize(writer, value.GetValueOrDefault(default(T)!), options);
        }
        else
        {
            writer.WriteBoolean("hasValue", false);
        }

        writer.WriteEndObject();
    }
}
