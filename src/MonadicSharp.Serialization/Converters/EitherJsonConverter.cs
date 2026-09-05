#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonadicSharp.Serialization.Converters;

/// <summary>
/// STJ factory that produces <see cref="EitherJsonConverter{TLeft,TRight}"/> for any <c>Either&lt;TLeft,TRight&gt;</c>.
/// Register via <see cref="SerializationExtensions.AddMonadicSharp"/>.
/// </summary>
public sealed class EitherJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(Either<,>);

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var args = typeToConvert.GetGenericArguments();
        var converterType = typeof(EitherJsonConverter<,>).MakeGenericType(args[0], args[1]);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// Serializes <c>Either&lt;TLeft, TRight&gt;</c> as:
/// <list type="bullet">
/// <item>Right (success path): <c>{"isRight":true,"value":&lt;TRight&gt;}</c></item>
/// <item>Left (failure path): <c>{"isRight":false,"left":&lt;TLeft&gt;}</c></item>
/// </list>
/// </summary>
public sealed class EitherJsonConverter<TLeft, TRight> : JsonConverter<Either<TLeft, TRight>>
{
    /// <inheritdoc/>
    public override Either<TLeft, TRight> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("isRight", out var isRightProp))
            throw new JsonException("Expected 'isRight' property in Either JSON.");

        if (isRightProp.GetBoolean())
        {
            if (!root.TryGetProperty("value", out var valueProp))
                throw new JsonException("Expected 'value' property in Right Either JSON.");

            var right = JsonSerializer.Deserialize<TRight>(valueProp.GetRawText(), options)
                        ?? throw new JsonException("Either right value deserialized to null.");
            return Either<TLeft, TRight>.FromRight(right);
        }
        else
        {
            if (!root.TryGetProperty("left", out var leftProp))
                throw new JsonException("Expected 'left' property in Left Either JSON.");

            var left = JsonSerializer.Deserialize<TLeft>(leftProp.GetRawText(), options)
                       ?? throw new JsonException("Either left value deserialized to null.");
            return Either<TLeft, TRight>.FromLeft(left);
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Either<TLeft, TRight> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        value.Match(
            onLeft: left =>
            {
                writer.WriteBoolean("isRight", false);
                writer.WritePropertyName("left");
                JsonSerializer.Serialize(writer, left, options);
            },
            onRight: right =>
            {
                writer.WriteBoolean("isRight", true);
                writer.WritePropertyName("value");
                JsonSerializer.Serialize(writer, right, options);
            });

        writer.WriteEndObject();
    }
}
