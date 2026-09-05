# MonadicSharp.Query

LINQ-style operators on `IEnumerable<Result<T>>` for batch LLM pipelines and multi-source data reconciliation.

```bash
dotnet add package MonadicSharp.Query
```

## Batch partitioning

```csharp
using MonadicSharp.Query.Extensions;

// Quality gate — require ≥80% success rate from an LLM batch
var outcome = llmResults.RequireSuccessRate(0.8);

// Success rate metric
double rate = llmResults.SuccessRate();

// Split values and errors
var values = results.SuccessValues();
var errors = results.FailureErrors();

// Project successes, discard errors
var (dtos, failures) = results.PartitionMap(r => r.ToDto());
```

## Multi-source reconciliation

```csharp
// Reconcile two LLM sources by entity ID, preferring the left source on conflict
var reconciled = primaryResults.MergeBy(fallbackResults, r => r.Id);

// Full outer reconcile with custom merge strategy
var merged = left.ReconcileBy(
    right,
    leftKey:   l => l.UserId,
    rightKey:  r => r.UserId,
    leftOnly:  l => new Dto(l),
    rightOnly: r => new Dto(r),
    merge:     (l, r) => new Dto(l, r));

// Pick best from competing model calls
var best = results.BestOf(r => r.ConfidenceScore);
```

## Group failures by type

```csharp
var failures  = results.GroupFailuresByType();
var notFound  = failures[ErrorType.NotFound];
var validErrs = failures[ErrorType.Validation];
var values    = results.SuccessValues();
```
