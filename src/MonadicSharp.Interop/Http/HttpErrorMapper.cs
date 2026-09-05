#nullable enable
using System.Net;

namespace MonadicSharp.Interop.Http;

/// <summary>
/// Maps <see cref="HttpStatusCode"/> values to typed <see cref="Error"/> instances
/// following MonadicSharp's <see cref="ErrorType"/> conventions.
/// </summary>
public static class HttpErrorMapper
{
    /// <summary>
    /// Maps an HTTP status code and optional reason phrase to a structured <see cref="Error"/>.
    /// </summary>
    public static Error Map(HttpStatusCode statusCode, string? reason = null)
    {
        var code = $"HTTP_{(int)statusCode}";
        var message = reason ?? statusCode.ToString();

        return (int)statusCode switch
        {
            400 => Error.Validation(message, code),
            401 => Error.Forbidden($"Unauthorized: {message}"),
            403 => Error.Forbidden(message),
            404 => Error.NotFound(message, code),
            409 => Error.Conflict(message, code),
            _ when (int)statusCode >= 400 && (int)statusCode < 500
                => Error.Create(message, code, ErrorType.Validation),
            _ when (int)statusCode >= 500
                => Error.Create(message, code, ErrorType.Failure),
            _   => Error.Create(message, code)
        };
    }
}
