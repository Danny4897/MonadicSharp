#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonadicSharp.Serialization.Converters;

/// <summary>
/// STJ factory that produces <see cref="ResultJsonConverter{T}"/> for any <c>Result&lt;T&gt;</c>.
/// Register via <see cref="SerializationExtensions.AddMonadicSharp"/>.
/// </summary>
public sealed class ResultJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(Result<>);

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ResultJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// Serializes <c>Result&lt;T&gt;</c> as:
/// <list type="bullet">
/// <item>Success: <c>{"ok":true,"value":&lt;T&gt;}</c></item>
/// <item>Failure: <c>{"ok":false,"error":"&lt;message&gt;","code":"&lt;code&gt;","type":"&lt;type&gt;"}</c></item>
/// </list>
/// </summary>
public sealed class ResultJsonConverter<T> : JsonConverter<Result<T>>
{
    /// <inheritdoc/>
    public override Result<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("ok", out var okProp))
            throw new JsonException("Expected 'ok' property in Result JSON.");

        if (okProp.GetBoolean())
        {
            if (!root.TryGetProperty("value", out var valueProp))
                throw new JsonException("Expected 'value' property in successful Result JSON.");

            var value = JsonSerializer.Deserialize<T>(valueProp.GetRawText(), options)
                        ?? throw new JsonException("Result value deserialized to null.");
            return Result<T>.Success(value);
        }
        else
        {
            var message = root.TryGetProperty("error", out var errProp)
                ? errProp.GetString() ?? "Unknown error"
                : "Unknown error";

            var code = root.TryGetProperty("code", out var codeProp)
                ? codeProp.GetString()
                : null;

            ErrorType type = ErrorType.Failure;
            if (root.TryGetProperty("type", out var typeProp) &&
                Enum.TryParse<ErrorType>(typeProp.GetString(), ignoreCase: true, out var parsedType))
                type = parsedType;

            return Result<T>.Failure(Error.Create(message, code, type));
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Result<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.IsSuccess)
        {
            writer.WriteBoolean("ok", true);
            writer.WritePropertyName("value");
            JsonSerializer.Serialize(writer, value.Value, options);
        }
        else
        {
            var error = value.Error;
            writer.WriteBoolean("ok", false);
            writer.WriteString("error", error.Message);
            writer.WriteString("code", error.Code);
            writer.WriteString("type", error.Type.ToString());
        }

        writer.WriteEndObject();
    }
}
