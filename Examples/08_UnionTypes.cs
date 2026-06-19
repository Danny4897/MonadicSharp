// Examples — documentation only, not compiled.
// ReSharper disable All
#pragma warning disable CS0168, CS8019, CS1998
using MonadicSharp;
using MonadicSharp.Extensions;
using MonadicSharp.Unions;
using MonadicSharp.Unions.Extensions;

namespace MonadicSharp.Examples;

// ============================================================
// 08 — UNION TYPES  ·  MonadicSharp.Unions
//      Discriminated unions for modeling multi-case outcomes
//      Bridge to C# 15 native Discriminated Unions
// ============================================================
//
//  Union2<T1, T2>         — two-case union  (e.g. Success | Error)
//  Union3<T1, T2, T3>     — three-case union
//  Union4<T1, T2, T3, T4> — four-case union
//
//  All support: Match (exhaustive), Map (on T1), Bind (on T1),
//               ToEither, implicit conversions, async variants
//
//  Common LLM patterns:
//    Union3<T, RateLimitInfo, ModelErrorInfo> for inference responses
//    Union2<string, byte[]> for multimodal payloads
//    Union4<T, T, T, Error> for multi-model fan-out
// ============================================================

static class UnionTypeExamples
{
    // ── Domain types ──────────────────────────────────────────────────────

    record InferenceResult(string Text, double Confidence, int TokensUsed);
    record RateLimitInfo(TimeSpan RetryAfter, int RemainingTokens);
    record ModelErrorInfo(string ModelId, string ErrorCode, string Detail);
    record ChunkData(string Content);

    // ── 8a. Union3 for LLM response modeling ──────────────────────────────
    //
    //  LLM calls can return one of three outcomes:
    //    Case1 = Inference succeeded
    //    Case2 = Rate limited (retry after N seconds)
    //    Case3 = Model error (wrong model, content policy, etc.)
    //
    //  Compare to: returning Task<Result<T>> which loses the rate-limit info.

    static Union3<InferenceResult, RateLimitInfo, ModelErrorInfo> CallLlm(string prompt, int attempt)
    {
        if (attempt == 0) return new RateLimitInfo(TimeSpan.FromSeconds(30), 0);
        if (prompt.Contains("forbidden")) return new ModelErrorInfo("gpt-x", "CONTENT_POLICY", "Prompt flagged");
        return new InferenceResult($"Completion for: {prompt}", 0.92, 1234);
    }

    static void HandleLlmResponse()
    {
        var response = CallLlm("Summarise this document.", attempt: 1);

        // Exhaustive match — compiler ensures all cases handled
        string display = response.Match(
            onCase1: r  => $"✓ {r.Text} (confidence: {r.Confidence:P0})",
            onCase2: rl => $"⏳ Rate limited. Retry after {rl.RetryAfter.TotalSeconds}s",
            onCase3: e  => $"✗ Model error [{e.ModelId}]: {e.ErrorCode} — {e.Detail}"
        );

        Console.WriteLine(display);
    }

    // ── 8b. Map on the success path ───────────────────────────────────────

    static Union3<string, RateLimitInfo, ModelErrorInfo> ExtractText(string prompt, int attempt) =>
        CallLlm(prompt, attempt)
            .Map(r => r.Text.Trim());  // maps InferenceResult→string, propagates T2/T3 unchanged

    // ── 8c. Convert to Result<T> at the service boundary ─────────────────

    static Result<InferenceResult> ToResult(Union3<InferenceResult, RateLimitInfo, ModelErrorInfo> u) =>
        u.ToResult(
            case2Error: rl  => Error.Create($"Rate limited: retry after {rl.RetryAfter.TotalSeconds}s", "RATE_LIMIT"),
            case3Error: err => Error.Create(err.Detail, err.ErrorCode)
        );

    // From UnionExtensions: LLM helpers
    static Union3<InferenceResult, RateLimitInfo, ModelErrorInfo> LlmSuccess(InferenceResult r) =>
        UnionExtensions.LlmSuccess<InferenceResult>(r);

    static Union3<InferenceResult, RateLimitInfo, ModelErrorInfo> LlmRateLimit(TimeSpan retryAfter) =>
        UnionExtensions.LlmRateLimit<InferenceResult>(retryAfter);

    // ── 8d. Union2 for dual-format content ───────────────────────────────

    // Content can be text or binary
    static Union2<string, byte[]> LoadContent(string path) =>
        path.EndsWith(".txt") ? File.ReadAllText(path) : File.ReadAllBytes(path);

    static int GetContentSize(string path) =>
        LoadContent(path).Match(
            onCase1: text  => text.Length,
            onCase2: bytes => bytes.Length
        );

    // ── 8e. Union4 for multi-model fan-out ────────────────────────────────

    record ModelAResult(string Text, double ConfA);
    record ModelBResult(string Text, double ConfB);
    record ModelCResult(string Text, double ConfC);

    static Union4<ModelAResult, ModelBResult, ModelCResult, ModelErrorInfo> CallModel(
        string prompt, string modelId) =>
        modelId switch
        {
            "A" => new ModelAResult($"A: {prompt}", 0.90),
            "B" => new ModelBResult($"B: {prompt}", 0.85),
            "C" => new ModelCResult($"C: {prompt}", 0.88),
            _   => new ModelErrorInfo(modelId, "UNKNOWN_MODEL", $"Model {modelId} not supported")
        };

    static string BestResponse(string prompt)
    {
        var results = new[] { "A", "B", "C" }
            .Select(id => CallModel(prompt, id))
            .Select(u => u.Match(
                onCase1: a => (score: a.ConfA, text: a.Text),
                onCase2: b => (score: b.ConfB, text: b.Text),
                onCase3: c => (score: c.ConfC, text: c.Text),
                onCase4: e => (score: 0.0,     text: string.Empty)
            ))
            .Where(r => r.score > 0)
            .OrderByDescending(r => r.score)
            .FirstOrDefault();

        return results.text.Length > 0 ? results.text : "All models failed";
    }

    // ── 8f. Async Match with ValueTask (.NET 11+) ─────────────────────────

    static async ValueTask<string> HandleAsync(
        Union3<InferenceResult, RateLimitInfo, ModelErrorInfo> u) =>
        await u.MatchAsync(
            onCase1: async r  => { await LogResultAsync(r);  return r.Text; },
            onCase2: async rl => { await Task.Delay(rl.RetryAfter); return string.Empty; },
            onCase3: async e  => { await LogErrorAsync(e);   return string.Empty; }
        );

    static Task LogResultAsync(InferenceResult r) => Task.CompletedTask;
    static Task LogErrorAsync(ModelErrorInfo e)   => Task.CompletedTask;
}
