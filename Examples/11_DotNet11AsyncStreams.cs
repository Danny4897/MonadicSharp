// Examples — documentation only, not compiled.
// ReSharper disable All
#pragma warning disable CS0168, CS8019, CS1998
using MonadicSharp;
using MonadicSharp.Extensions;
using MonadicSharp.Interop.Http;
using MonadicSharp.Query.Extensions;

namespace MonadicSharp.Examples;

// ============================================================
// 11 — .NET 11 ASYNC STREAMS + VALUETASK
//      IAsyncEnumerable<Result<T>> for streaming LLM pipelines
//      ValueTask<Result<T>> for Runtime Async zero-alloc hot paths
// ============================================================
//
//  .NET 11 Runtime Async eliminates state-machine allocation for
//  async methods — ValueTask is the preferred return type on hot paths.
//
//  AsyncStreamExtensions (NET11+):
//    MapEach / BindEach / BindEachAsync
//    SuccessValues / TakeUntilError
//    CollectAsync / CollectValuesAsync
//    TraverseAsync
//
//  AsyncQueryExtensions (NET11+):
//    PartitionAsync / SuccessValuesAsync
//    RequireSuccessRateAsync
//    BestOfAsync
//
//  SseExtensions (NET11+):
//    GetSseStream<T> / PostSseStream<TReq,T> / SendSseStream<T>
// ============================================================

#if NET11_0_OR_GREATER
static class DotNet11Examples
{
    record DocumentChunk(string Id, string Text);
    record SummaryToken(string ChunkId, string Token, bool IsLast);
    record SummaryChunk(string Id, string Summary, double Confidence);
    record EmbeddingChunk(string Id, float[] Embedding);

    // ── 11a. Streaming LLM inference via SSE ──────────────────────────────

    static async IAsyncEnumerable<Result<SummaryToken>> StreamSummaryTokensAsync(
        HttpClient client, string documentId, string text,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new { documentId, text, stream = true };

        // LLM SSE stream → IAsyncEnumerable<Result<SummaryToken>>
        await foreach (var token in client.PostSseStream<object, SummaryToken>(
            "/llm/summarise", request, ct: ct).WithCancellation(ct))
        {
            yield return token;
        }
    }

    // ── 11b. Process streaming tokens with pipeline operators ─────────────

    static async Task<Result<string>> AssembleSummaryAsync(
        HttpClient client, string docId, string text, CancellationToken ct = default)
    {
        var tokens = StreamSummaryTokensAsync(client, docId, text, ct);

        // Stop the stream at the first error (strict pipeline)
        var collected = await tokens
            .TakeUntilError(ct)           // yields up to first failure
            .CollectAsync(ct);            // → Result<IReadOnlyList<SummaryToken>>

        return collected.Map(ts => string.Concat(ts.Select(t => t.Token)));
    }

    // ── 11c. Fan-out: process multiple documents concurrently ──────────────

    static async Task<IReadOnlyList<Result<string>>> ProcessBatchStreamAsync(
        HttpClient client, IEnumerable<DocumentChunk> chunks, CancellationToken ct = default)
    {
        var tasks = chunks.Select(c =>
            AssembleSummaryAsync(client, c.Id, c.Text, ct).AsTask());

        return await Task.WhenAll(tasks);
    }

    // ── 11d. AsyncQueryExtensions — quality gate on a stream ──────────────

    static async Task<Result<IReadOnlyList<SummaryChunk>>> BatchWithQualityGate(
        IAsyncEnumerable<Result<SummaryChunk>> stream, CancellationToken ct = default)
    {
        // Must have ≥80% success rate across the stream
        return await stream.RequireSuccessRateAsync(0.80, ct);
    }

    // ── 11e. Async partition — process successes, buffer errors ───────────

    static async Task PartitionStreamAsync(
        IAsyncEnumerable<Result<SummaryChunk>> stream, CancellationToken ct = default)
    {
        var (successes, failures) = await stream.PartitionAsync(ct);

        Console.WriteLine($"Successes: {successes.Count}, Failures: {failures.Count}");

        foreach (var err in failures)
            Console.WriteLine($"  [{err.Type}] {err.Code}: {err.Message}");
    }

    // ── 11f. ValueTask<Result<T>> pipeline (Runtime Async zero-alloc) ──────

    static async ValueTask<Result<SummaryChunk>> SummariseChunkAsync(DocumentChunk chunk)
    {
        // Simulated LLM call — ValueTask avoids state machine allocation on fast paths
        await Task.Delay(1);
        return new SummaryChunk(chunk.Id, $"Summary: {chunk.Text[..Math.Min(50, chunk.Text.Length)]}", 0.91);
    }

    static async ValueTask<Result<EmbeddingChunk>> EmbedChunkAsync(SummaryChunk summary)
    {
        await Task.Delay(1);
        return new EmbeddingChunk(summary.Id, new float[1536]);
    }

    static async ValueTask<Result<EmbeddingChunk>> FullPipelineAsync(DocumentChunk chunk)
    {
        // ValueTask<Result<T>> pipeline — zero state-machine alloc on .NET 11
        return await SummariseChunkAsync(chunk)
            .Bind(s  => EmbedChunkAsync(s))
            .Do(e    => Console.WriteLine($"Embedded {e.Id}: [{e.Embedding.Length} dims]"))
            .OrElse(err => new EmbeddingChunk(chunk.Id, Array.Empty<float>()).AsValueTask());
    }

    // ── 11g. Stream → map → collect pattern ───────────────────────────────

    static async Task<Result<IReadOnlyList<EmbeddingChunk>>> StreamProcessAndCollect(
        IAsyncEnumerable<DocumentChunk> source, CancellationToken ct = default)
    {
        return await source
            .TraverseAsync(async chunk =>        // IAsyncEnumerable<DocumentChunk> → IAsyncEnumerable<Result<SummaryChunk>>
                await SummariseChunkAsync(chunk).AsTask(), ct)
            .BindEachAsync(async s =>            // chain: SummaryChunk → Result<EmbeddingChunk>
                await EmbedChunkAsync(s).AsTask(), ct)
            .SuccessValues(ct: ct)               // IAsyncEnumerable<EmbeddingChunk> (errors discarded)
            .CollectListAsync(ct);               // → Result<IReadOnlyList<EmbeddingChunk>>
    }

    // Helper: collect IAsyncEnumerable<T> (non-Result) to list
    static async ValueTask<Result<IReadOnlyList<T>>> CollectListAsync<T>(
        this IAsyncEnumerable<T> source, CancellationToken ct = default)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(ct).ConfigureAwait(false))
            list.Add(item);
        return Result<IReadOnlyList<T>>.Success(list);
    }

    // ── 11h. Best-of from async stream ────────────────────────────────────

    static async ValueTask<Result<SummaryChunk>> BestSummaryAsync(
        IAsyncEnumerable<Result<SummaryChunk>> stream, CancellationToken ct = default)
    {
        // Pick highest-confidence chunk from the stream without buffering all
        return await stream.BestOfAsync(s => s.Confidence, ct);
    }
}
#endif
