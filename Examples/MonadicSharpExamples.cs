// ============================================================
//  MonadicSharp — Examples Index
// ============================================================
//
//  File                         Topic
//  ─────────────────────────────────────────────────────────
//  01_BasicResult.cs            Result<T>: Map, Bind, Match, Do, Try, Combine
//  02_OptionMaybe.cs            Option<T>: Map, Bind, Filter, Sequence, ToResult
//  03_ValidationPipeline.cs     Multi-field validation: fail-fast vs accumulate
//  04_RepositoryDdd.cs          DDD: domain rules + Option repo + Result service
//  05_MinimalApiInterop.cs      ASP.NET Core Minimal API + ToOkResult/ToCreated
//  06_LlmBatchPipeline.cs       Batch LLM: Partition, RequireSuccessRate, BestOf
//  07_HttpInterop.cs            HttpClient: GetAsResult, PostAsResult, retry
//  08_UnionTypes.cs             Union2/3/4: LLM response modeling, fan-out
//  09_Serialization.cs          STJ converters: Result/Option JSON round-trip
//  10_AspireObservability.cs    Circuit breaker + GreenScore + distributed traces
//  11_DotNet11AsyncStreams.cs   .NET 11: IAsyncEnumerable<Result<T>>, ValueTask
//
//  Quick-start:
//    dotnet add package MonadicSharp
//
//  For HTTP, Minimal API bridge:
//    dotnet add package MonadicSharp.Interop
//
//  For LLM batch operators (Partition, Reconcile, BestOf):
//    dotnet add package MonadicSharp.Query
//
//  For Union2/3/4 discriminated unions:
//    dotnet add package MonadicSharp.Unions
//
//  For STJ converters:
//    dotnet add package MonadicSharp.Serialization
//
//  For Aspire health + OTel metrics:
//    dotnet add package MonadicSharp.Aspire
// ============================================================
