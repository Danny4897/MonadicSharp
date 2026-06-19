#nullable enable
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MonadicSharp.Interop.Http;

#if NET11_0_OR_GREATER
/// <summary>
/// Server-Sent Events (SSE) streaming extensions for <see cref="HttpClient"/>.
/// Converts SSE streams from LLM inference endpoints (OpenAI, Azure OpenAI, Anthropic)
/// into <c>IAsyncEnumerable&lt;Result&lt;T&gt;&gt;</c> pipelines on .NET 11+.
/// </summary>
public static class SseExtensions
{
    private const string DataPrefix = "data: ";
    private const string DoneMarker = "[DONE]";

    /// <summary>
    /// Streams a GET request as Server-Sent Events, deserializing each <c>data:</c> line
    /// into <typeparamref name="T"/>. Each event becomes a <c>Result&lt;T&gt;</c>:
    /// deserialization failures are <see cref="ErrorType.Validation"/> errors, not exceptions.
    /// The <c>[DONE]</c> sentinel (used by OpenAI-compatible APIs) terminates the stream.
    /// </summary>
    public static IAsyncEnumerable<Result<T>> GetSseStream<T>(
        this HttpClient client,
        string requestUri,
        JsonSerializerOptions? options = null,
        CancellationToken ct = default) =>
        client.SendSseStream<T>(new HttpRequestMessage(HttpMethod.Get, requestUri), options, ct);

    /// <summary>
    /// Streams a POST request (e.g. <c>stream: true</c> LLM completion) as SSE events.
    /// Serializes <paramref name="payload"/> as JSON in the request body.
    /// </summary>
    public static IAsyncEnumerable<Result<T>> PostSseStream<TRequest, T>(
        this HttpClient client,
        string requestUri,
        TRequest payload,
        JsonSerializerOptions? options = null,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(payload, options: options)
        };
        return client.SendSseStream<T>(request, options, ct);
    }

    /// <summary>
    /// Sends <paramref name="request"/> and streams the response body as SSE events,
    /// deserializing each <c>data:</c> line into <typeparamref name="T"/>.
    /// </summary>
    public static async IAsyncEnumerable<Result<T>> SendSseStream<T>(
        this HttpClient client,
        HttpRequestMessage request,
        JsonSerializerOptions? options = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            yield return Result<T>.Failure(Error.FromException(ex));
            yield break;
        }

        if (!response.IsSuccessStatusCode)
        {
            yield return Result<T>.Failure(HttpErrorMapper.Map(response.StatusCode, response.ReasonPhrase));
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                yield return Result<T>.Failure(Error.FromException(ex));
                yield break;
            }

            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith(DataPrefix, StringComparison.Ordinal)) continue;

            var data = line[DataPrefix.Length..];
            if (data == DoneMarker) yield break;

            Result<T> parsed;
            try
            {
                var value = JsonSerializer.Deserialize<T>(data, options);
                parsed = value is null
                    ? Result<T>.Failure(Error.Create("SSE event deserialized to null.", "SSE_NULL"))
                    : Result<T>.Success(value);
            }
            catch (JsonException ex)
            {
                parsed = Result<T>.Failure(
                    Error.Create($"SSE JSON parse error: {ex.Message}", "SSE_JSON_ERROR", ErrorType.Validation));
            }

            yield return parsed;
        }
    }
}
#endif
