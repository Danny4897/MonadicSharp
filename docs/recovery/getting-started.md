# Getting Started

[![NuGet](https://img.shields.io/nuget/v/MonadicSharp.Recovery.svg?style=flat-square)](https://www.nuget.org/packages/MonadicSharp.Recovery/) [![NuGet Downloads](https://img.shields.io/nuget/dt/MonadicSharp.Recovery.svg?style=flat-square)](https://www.nuget.org/packages/MonadicSharp.Recovery/)


MonadicSharp.Recovery adds two recovery operators to [MonadicSharp](https://danny4897.github.io/MonadicSharp/) pipelines — `RescueAsync` for single-attempt recovery and `StartFixBranchAsync` for multi-attempt recovery.

## Install

```bash
dotnet add package MonadicSharp.Recovery
```

**Requires**: .NET 8.0+, MonadicSharp ≥ 1.5.

## RescueAsync — single recovery

```csharp
using MonadicSharp.Recovery;

var result = await FetchUserAsync(userId)
    .RescueAsync(
        when: err => err is UserError.NotFound,
        recover: _ => CreateGuestUserAsync());

// If FetchUserAsync fails with UserError.NotFound → CreateGuestUserAsync() is tried.
// If CreateGuestUserAsync also fails → the new error propagates.
// All other errors from FetchUserAsync pass through unchanged.
```

## StartFixBranchAsync — multi-attempt recovery

```csharp
var result = await CallExternalApiAsync(request)
    .StartFixBranchAsync(
        when: err => err is ApiError.RateLimit or ApiError.Timeout,
        maxAttempts: 3,
        delay: TimeSpan.FromSeconds(2),
        fix: _ => CallExternalApiAsync(request));

// Attempts recovery up to 3 times with 2-second delays.
// On exhaustion, returns the last error from the fix attempts.
```

## With telemetry

```csharp
var telemetry = new MyRecoveryTelemetry(logger);

var result = await FetchDataAsync()
    .RescueAsync(
        when: err => err is DataError.Stale,
        recover: _ => RefreshAndFetchAsync(),
        telemetry: telemetry);  // optional
```

## Three-lane model

```
Input → [ Green track: success path ] → Output ✅
            ↓ on matching error
         [ Amber track: recovery ]
            ↓ on recovery success    ↓ on recovery failure
         [ Back to Green ✅ ]      [ Red track ❌ ]
```

## Next steps

- [The Three Lanes](./three-lanes) — conceptual overview
- [RescueAsync API](./api/rescue-async) — full parameter reference
- [StartFixBranchAsync API](./api/start-fix-branch-async) — multi-attempt recovery reference
