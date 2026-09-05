// Examples — documentation only, not compiled.
// ReSharper disable All
#pragma warning disable CS0168, CS8019, CS1998
using System.Text.Json;
using MonadicSharp;
using MonadicSharp.Extensions;
using MonadicSharp.Serialization;

namespace MonadicSharp.Examples;

// ============================================================
// 09 — JSON SERIALIZATION  ·  MonadicSharp.Serialization
//      AOT-safe System.Text.Json converters for Result/Option/Either
//      Zero global state — converters registered per JsonSerializerOptions
// ============================================================
//
//  Wire formats:
//
//  Result<T>  success:  { "ok": true,  "value": <T> }
//  Result<T>  failure:  { "ok": false, "error": { "code": "...", "message": "...",
//                                                  "type": "Validation", ... } }
//
//  Option<T>  some:     { "hasValue": true,  "value": <T> }
//  Option<T>  none:     { "hasValue": false }
//
//  Either<L,R> right:   { "isRight": true,  "value": <R> }
//  Either<L,R> left:    { "isRight": false, "left": <L>  }
// ============================================================

static class SerializationExamples
{
    record UserProfile(string Name, string Email, int Age);

    // ── 9a. Setup ─────────────────────────────────────────────────────────

    static readonly JsonSerializerOptions Options =
        SerializationExtensions.CreateDefaultOptions();
    // Includes all MonadicSharp converters + Web defaults (camelCase, ignore null, etc.)

    // Manual setup for existing options:
    static JsonSerializerOptions ConfigureExistingOptions()
    {
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opts.AddMonadicSharp(); // chainable
        return opts;
    }

    // ── 9b. Serialize Result<T> ───────────────────────────────────────────

    static void SerializeResult()
    {
        var success = Result<UserProfile>.Success(new("Alice", "alice@example.com", 30));
        var failure = Result<UserProfile>.Failure(Error.NotFound("User", "42"));

        string successJson = JsonSerializer.Serialize(success, Options);
        // {"ok":true,"value":{"name":"Alice","email":"alice@example.com","age":30}}

        string failureJson = JsonSerializer.Serialize(failure, Options);
        // {"ok":false,"error":{"code":"NOT_FOUND","message":"User not found","type":"NotFound"}}
    }

    // ── 9c. Deserialize Result<T> ─────────────────────────────────────────

    static void DeserializeResult()
    {
        const string json = """{"ok":true,"value":{"name":"Bob","email":"bob@example.com","age":25}}""";

        var result = JsonSerializer.Deserialize<Result<UserProfile>>(json, Options);
        // result.IsSuccess == true, result.Value.Name == "Bob"

        const string failJson = """{"ok":false,"error":{"code":"VALIDATION_ERROR","message":"Email required","type":"Validation"}}""";
        var failed = JsonSerializer.Deserialize<Result<UserProfile>>(failJson, Options);
        // failed.IsFailure == true, failed.Error.Type == ErrorType.Validation
    }

    // ── 9d. Extension convenience methods ─────────────────────────────────

    static void ExtensionMethods()
    {
        var result = Result<UserProfile>.Success(new("Alice", "alice@example.com", 30));

        // Direct serialization from Result<T>
        string json = result.ToJson(Options);

        // Deserialize from string
        Result<UserProfile> restored = SerializationExtensions.FromJson<UserProfile>(json, Options);
    }

    // ── 9e. Serialize Option<T> ───────────────────────────────────────────

    static void SerializeOption()
    {
        var some = Option<string>.Some("hello");
        var none = Option<string>.None;

        string someJson = JsonSerializer.Serialize(some, Options);
        // {"hasValue":true,"value":"hello"}

        string noneJson = JsonSerializer.Serialize(none, Options);
        // {"hasValue":false}
    }

    // ── 9f. API response with Result<T> ───────────────────────────────────

    // Controller returns Result<T> — serialized as monadic JSON automatically
    // when MonadicSharp converters are registered in ASP.NET Core:
    //
    //   builder.Services.ConfigureHttpJsonOptions(opts =>
    //       opts.SerializerOptions.AddMonadicSharp());
    //
    // GET /users/1  →  200 OK: {"ok":true,"value":{"name":"Alice",...}}
    // GET /users/0  →  200 OK: {"ok":false,"error":{"code":"NOT_FOUND",...}}
    //   (status 200 always — client inspects "ok" to distinguish success/failure)
    //
    // Or use MonadicSharp.Interop's .ToOkResult() for status-code-based responses.

    // ── 9g. Bidirectional round-trip ──────────────────────────────────────

    static void RoundTrip()
    {
        var original = Result<UserProfile>.Success(new("Charlie", "c@example.com", 28));

        var json     = original.ToJson(Options);
        var restored = SerializationExtensions.FromJson<UserProfile>(json, Options);

        bool equal = original.IsSuccess == restored.IsSuccess
                  && original.Value.Name == restored.Value.Name;
        Console.WriteLine($"Round-trip OK: {equal}");
    }

    // ── 9h. Service Bus / queue serialization ─────────────────────────────
    //
    //  Serialize Result<T> to queue message body, deserialize on consumer side.
    //  Pattern: Producer sends Result<T> JSON; Consumer reads and continues pipeline.

    static async Task ServiceBusProducer(Result<UserProfile> result, Stream outputStream)
    {
        await JsonSerializer.SerializeAsync(outputStream, result, Options);
    }

    static async Task<Result<UserProfile>> ServiceBusConsumer(Stream inputStream)
    {
        return await JsonSerializer.DeserializeAsync<Result<UserProfile>>(inputStream, Options)
               ?? Result<UserProfile>.Failure(Error.Create("Empty message body"));
    }
}
