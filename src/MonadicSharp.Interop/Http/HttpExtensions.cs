#nullable enable
using System.Net.Http.Json;
using System.Text.Json;
using MonadicSharp.Interop.Http;

namespace MonadicSharp.Interop;

/// <summary>
/// Extension methods bridging <see cref="HttpResponseMessage"/> with MonadicSharp primitives.
/// On .NET 10+ these are surfaced as C# 14 extension members directly on
/// <see cref="HttpResponseMessage"/>; on earlier targets they are standard extension methods.
/// </summary>
public static class HttpResponseMessageExtensions
{
#if NET10_0_OR_GREATER
    // C# 14 extension block — adds members directly onto HttpResponseMessage
    extension(HttpResponseMessage response)
    {
        /// <summary>
        /// Maps a successful response to <c>Result&lt;Unit&gt;</c> without reading the body.
        /// Non-success status codes become typed <see cref="Error"/> failures.
        /// </summary>
        public Result<Unit> ToResult() =>
            response.IsSuccessStatusCode
                ? Result<Unit>.Success(Unit.Value)
                : Result<Unit>.Failure(HttpErrorMapper.Map(response.StatusCode, response.ReasonPhrase));

        /// <summary>
        /// Reads the response body as <typeparamref name="T"/> and wraps it in <c>Result&lt;T&gt;</c>.
        /// Uses <see cref="HttpContentJsonExtensions.ReadFromJsonAsync{T}"/> internally.
        /// </summary>
        public async Task<Result<T>> ReadAsResult<T>(JsonSerializerOptions? options = null,
            CancellationToken ct = default)
        {
            if (!response.IsSuccessStatusCode)
                return Result<T>.Failure(HttpErrorMapper.Map(response.StatusCode, response.ReasonPhrase));

            try
            {
                var value = await response.Content.ReadFromJsonAsync<T>(options, ct);
                return value is null
                    ? Result<T>.Failure(Error.Create("Response body deserialized to null."))
                    : Result<T>.Success(value);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(Error.FromException(ex));
            }
        }

        /// <summary>
        /// Returns <c>true</c> when the response has a 2xx status code.
        /// Equivalent to <see cref="HttpResponseMessage.IsSuccessStatusCode"/> but
        /// surfaced as a MonadicSharp-aware computed property.
        /// </summary>
        public bool IsMonadicSuccess => response.IsSuccessStatusCode;
    }
#else
    /// <summary>
    /// Maps a successful response to <c>Result&lt;Unit&gt;</c> without reading the body.
    /// </summary>
    public static Result<Unit> ToResult(this HttpResponseMessage response) =>
        response.IsSuccessStatusCode
            ? Result<Unit>.Success(Unit.Value)
            : Result<Unit>.Failure(HttpErrorMapper.Map(response.StatusCode, response.ReasonPhrase));

    /// <summary>
    /// Reads the response body as <typeparamref name="T"/> and wraps it in <c>Result&lt;T&gt;</c>.
    /// </summary>
    public static async Task<Result<T>> ReadAsResult<T>(this HttpResponseMessage response,
        JsonSerializerOptions? options = null,
        CancellationToken ct = default)
    {
        if (!response.IsSuccessStatusCode)
            return Result<T>.Failure(HttpErrorMapper.Map(response.StatusCode, response.ReasonPhrase));

        try
        {
            var value = await response.Content.ReadFromJsonAsync<T>(options, ct);
            return value is null
                ? Result<T>.Failure(Error.Create("Response body deserialized to null."))
                : Result<T>.Success(value);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(Error.FromException(ex));
        }
    }
#endif
}

/// <summary>
/// Extension methods bridging <see cref="HttpClient"/> with MonadicSharp pipelines.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Sends a GET request and deserializes the body as <typeparamref name="T"/>,
    /// wrapping the outcome in <c>Result&lt;T&gt;</c>. Network failures become typed errors.
    /// </summary>
    public static async Task<Result<T>> GetAsResult<T>(this HttpClient client,
        string requestUri,
        JsonSerializerOptions? options = null,
        CancellationToken ct = default)
    {
        try
        {
            var response = await client.GetAsync(requestUri, ct);
#if NET10_0_OR_GREATER
            return await response.ReadAsResult<T>(options, ct);
#else
            return await HttpResponseMessageExtensions.ReadAsResult<T>(response, options, ct);
#endif
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(Error.FromException(ex));
        }
    }

    /// <summary>
    /// Sends a POST request with <paramref name="payload"/> serialized as JSON and wraps
    /// the response in <c>Result&lt;TResponse&gt;</c>. Network failures become typed errors.
    /// </summary>
    public static async Task<Result<TResponse>> PostAsResult<TRequest, TResponse>(
        this HttpClient client,
        string requestUri,
        TRequest payload,
        JsonSerializerOptions? options = null,
        CancellationToken ct = default)
    {
        try
        {
            var response = await client.PostAsJsonAsync(requestUri, payload, options, ct);
#if NET10_0_OR_GREATER
            return await response.ReadAsResult<TResponse>(options, ct);
#else
            return await HttpResponseMessageExtensions.ReadAsResult<TResponse>(response, options, ct);
#endif
        }
        catch (Exception ex)
        {
            return Result<TResponse>.Failure(Error.FromException(ex));
        }
    }
}
