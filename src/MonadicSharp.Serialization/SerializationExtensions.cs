#nullable enable
using System.Text.Json;
using MonadicSharp.Serialization.Converters;

namespace MonadicSharp.Serialization;

/// <summary>
/// Extension methods for registering MonadicSharp JSON converters.
/// </summary>
public static class SerializationExtensions
{
    /// <summary>
    /// Registers all MonadicSharp JSON converters on the provided <see cref="JsonSerializerOptions"/>.
    /// Call this during application startup — converters are not registered globally.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.Configure&lt;JsonOptions&gt;(opts =>
    ///     opts.JsonSerializerOptions.AddMonadicSharp());
    /// </code>
    /// </example>
    public static JsonSerializerOptions AddMonadicSharp(this JsonSerializerOptions options)
    {
        options.Converters.Add(new ResultJsonConverterFactory());
        options.Converters.Add(new OptionJsonConverterFactory());
        options.Converters.Add(new EitherJsonConverterFactory());
        return options;
    }

    /// <summary>
    /// Creates a new <see cref="JsonSerializerOptions"/> instance pre-configured with
    /// all MonadicSharp converters and camelCase naming.
    /// </summary>
    public static JsonSerializerOptions CreateDefaultOptions() =>
        new JsonSerializerOptions(JsonSerializerDefaults.Web).AddMonadicSharp();

    /// <summary>
    /// Serializes a <c>Result&lt;T&gt;</c> to a JSON string using the registered converters.
    /// </summary>
    public static string ToJson<T>(this Result<T> result, JsonSerializerOptions? options = null) =>
        JsonSerializer.Serialize(result, options ?? CreateDefaultOptions());

    /// <summary>
    /// Serializes an <c>Option&lt;T&gt;</c> to a JSON string using the registered converters.
    /// </summary>
    public static string ToJson<T>(this Option<T> option, JsonSerializerOptions? options = null) =>
        JsonSerializer.Serialize(option, options ?? CreateDefaultOptions());

    /// <summary>
    /// Deserializes a JSON string into a <c>Result&lt;T&gt;</c>.
    /// </summary>
    public static Result<T> FromJson<T>(string json, JsonSerializerOptions? options = null) =>
        JsonSerializer.Deserialize<Result<T>>(json, options ?? CreateDefaultOptions());

    /// <summary>
    /// Deserializes a JSON string into an <c>Option&lt;T&gt;</c>.
    /// </summary>
    public static Option<T> FromJsonOption<T>(string json, JsonSerializerOptions? options = null) =>
        JsonSerializer.Deserialize<Option<T>>(json, options ?? CreateDefaultOptions());
}
