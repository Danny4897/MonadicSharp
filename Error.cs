using System.Text.Json;

namespace MonadicSharp;

/// <summary>
/// Represents an immutable error with code, message and optional details.
/// Supports composition of multiple errors.
/// </summary>
public sealed record Error
{
    public string Code { get; init; }
    public string Message { get; init; }
    public ErrorType Type { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; }
    public Error? InnerError { get; init; }
    public IReadOnlyList<Error> SubErrors { get; init; }

    private Error(string code, string message, ErrorType type = ErrorType.Failure,
        IReadOnlyDictionary<string, object>? metadata = null, Error? innerError = null,
        IReadOnlyList<Error>? subErrors = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Type = type;
        Metadata = metadata ?? new Dictionary<string, object>();
        InnerError = innerError;
        SubErrors = subErrors ?? Array.Empty<Error>();
    }

    /// <summary>
    /// Creates a simple error
    /// </summary>
    public static Error Create(string message, string? code = null, ErrorType type = ErrorType.Failure) =>
        new(code ?? "GENERAL_ERROR", message, type);

    /// <summary>
    /// Creates a validation error
    /// </summary>
    public static Error Validation(string message, string? field = null) =>
        new("VALIDATION_ERROR", message, ErrorType.Validation,
            field != null ? new Dictionary<string, object> { ["Field"] = field } : null);

    /// <summary>
    /// Creates an error from exception
    /// </summary>
    public static Error FromException(Exception exception, string? code = null) =>
        new(code ?? exception.GetType().Name.ToUpperInvariant(),
            exception.Message, ErrorType.Exception,
            new Dictionary<string, object>
            {
                ["ExceptionType"] = exception.GetType().Name,
                ["StackTrace"] = exception.StackTrace ?? ""
            });

    /// <summary>
    /// Creates a not found error
    /// </summary>
    public static Error NotFound(string resource, string? identifier = null) =>
        new("NOT_FOUND", $"{resource} not found", ErrorType.NotFound,
            identifier != null ? new Dictionary<string, object> { ["Identifier"] = identifier } : null);

    /// <summary>
    /// Creates an access denied error
    /// </summary>
    public static Error Forbidden(string message = "Access denied") =>
        new("FORBIDDEN", message, ErrorType.Forbidden);

    /// <summary>
    /// Creates a conflict error
    /// </summary>
    public static Error Conflict(string message, string? resource = null) =>
        new("CONFLICT", message, ErrorType.Conflict,
            resource != null ? new Dictionary<string, object> { ["Resource"] = resource } : null);

    /// <summary>
    /// Combines multiple errors into one
    /// </summary>
    public static Error Combine(params Error[] errors)
    {
        if (errors.Length == 0)
            throw new ArgumentException("At least one error is required", nameof(errors));

        if (errors.Length == 1)
            return errors[0];

        return new("MULTIPLE_ERRORS", "Multiple errors occurred", ErrorType.Failure,
            subErrors: errors.ToList());
    }

    /// <summary>
    /// Adds metadata to the error
    /// </summary>
    public Error WithMetadata(string key, object value)
    {
        var newMetadata = new Dictionary<string, object>(Metadata) { [key] = value };
        return this with { Metadata = newMetadata };
    }

    /// <summary>
    /// Adds an inner error
    /// </summary>
    public Error WithInnerError(Error innerError) =>
        this with { InnerError = innerError };

    /// <summary>
    /// Checks if the error is of a specific type
    /// </summary>
    public bool IsOfType(ErrorType type) => Type == type;

    /// <summary>
    /// Checks if the error has a specific code
    /// </summary>
    public bool HasCode(string code) => Code.Equals(code, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets all errors (including sub-errors) as a flat list
    /// </summary>
    public IEnumerable<Error> GetAllErrors()
    {
        yield return this;

        foreach (var subError in SubErrors)
        {
            foreach (var error in subError.GetAllErrors())
            {
                yield return error;
            }
        }

        if (InnerError != null)
        {
            foreach (var error in InnerError.GetAllErrors())
            {
                yield return error;
            }
        }
    }

    /// <summary>
    /// Serializes the error to JSON
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    public override string ToString()
    {
        var result = $"[{Type}] {Code}: {Message}";

        if (Metadata.Any())
        {
            var metadata = string.Join(", ", Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            result += $" | Metadata: {metadata}";
        }

        if (SubErrors.Any())
        {
            result += $" | SubErrors: {SubErrors.Count}";
        }

        if (InnerError != null)
        {
            result += $" | Inner: {InnerError.Code}";
        }

        return result;
    }
}

/// <summary>
/// Supported error types
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Forbidden = 3,
    Conflict = 4,
    Exception = 5
}
