// Examples — documentation only, not compiled.
// ReSharper disable All
#pragma warning disable CS0168, CS8019, CS1998
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MonadicSharp;
using MonadicSharp.Extensions;
using MonadicSharp.Interop;
using MonadicSharp.Interop.AspNetCore;

namespace MonadicSharp.Examples;

// ============================================================
// 05 — ASP.NET CORE MINIMAL API + MonadicSharp.Interop
//      Map Result<T> to IResult / HTTP responses automatically
// ============================================================
//
//  MonadicSharp.Interop bridges Result<T> to:
//    • IResult (Minimal APIs)  — .ToOkResult(), .ToCreatedResult()
//    • ObjectResult (MVC)      — .ToActionResult()
//
//  ErrorType → HTTP status mapping:
//    Validation  → 400 Bad Request
//    NotFound    → 404 Not Found
//    Forbidden   → 403 Forbidden
//    Conflict    → 409 Conflict
//    Failure     → 500 Internal Server Error
// ============================================================

static class MinimalApiExamples
{
    record CreateProductRequest(string Name, decimal Price, int Stock);
    record ProductResponse(Guid Id, string Name, decimal Price, int Stock);
    record UpdateStockRequest(int Delta);

    // ── 5a. Service returning Result<T> ───────────────────────────────────

    class ProductService
    {
        private readonly Dictionary<Guid, ProductResponse> _store = new();

        public Result<ProductResponse> Create(CreateProductRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Error.Validation("Product name is required", field: "name");

            if (req.Price <= 0)
                return Error.Validation("Price must be positive", field: "price");

            if (req.Stock < 0)
                return Error.Validation("Stock cannot be negative", field: "stock");

            var product = new ProductResponse(Guid.NewGuid(), req.Name.Trim(), req.Price, req.Stock);
            _store[product.Id] = product;
            return product;
        }

        public Result<ProductResponse> GetById(Guid id) =>
            _store.TryGetValue(id, out var p)
                ? Result<ProductResponse>.Success(p)
                : Error.NotFound("Product", id.ToString());

        public Result<ProductResponse> UpdateStock(Guid id, int delta)
        {
            if (!_store.TryGetValue(id, out var product))
                return Error.NotFound("Product", id.ToString());

            var newStock = product.Stock + delta;
            if (newStock < 0)
                return Error.Conflict($"Insufficient stock: have {product.Stock}, reducing by {-delta}");

            var updated = product with { Stock = newStock };
            _store[id] = updated;
            return updated;
        }

        public Result<Unit> Delete(Guid id) =>
            _store.Remove(id)
                ? Result<Unit>.Success(Unit.Value)
                : Error.NotFound("Product", id.ToString());
    }

    // ── 5b. Minimal API endpoints ─────────────────────────────────────────

    static void MapProductEndpoints(WebApplication app)
    {
        var svc = new ProductService();

        // GET /products/{id}
        // Result<T>.ToOkResult() → 200 OK or typed error status
        app.MapGet("/products/{id:guid}", (Guid id) =>
            svc.GetById(id).ToOkResult());

        // POST /products
        // .ToCreatedResult(id => $"/products/{id}") → 201 Created with Location header
        app.MapPost("/products", (CreateProductRequest req) =>
            svc.Create(req).ToCreatedResult(p => $"/products/{p.Id}"));

        // PATCH /products/{id}/stock
        app.MapPatch("/products/{id:guid}/stock", (Guid id, UpdateStockRequest req) =>
            svc.UpdateStock(id, req.Delta).ToOkResult());

        // DELETE /products/{id}
        app.MapDelete("/products/{id:guid}", (Guid id) =>
            svc.Delete(id).ToOkResult());
    }

    // ── 5c. With async service ────────────────────────────────────────────

    static void MapAsyncEndpoints(WebApplication app, IAsyncProductService asyncSvc)
    {
        // Async endpoints — same .ToOkResult() bridge works
        app.MapGet("/v2/products/{id:guid}", async (Guid id, CancellationToken ct) =>
            (await asyncSvc.GetByIdAsync(id, ct)).ToOkResult());

        app.MapPost("/v2/products", async (CreateProductRequest req, CancellationToken ct) =>
            (await asyncSvc.CreateAsync(req, ct)).ToCreatedResult(p => $"/v2/products/{p.Id}"));
    }

    // ── 5d. Endpoint filter — centralized error logging ───────────────────
    //
    //  Register via:  app.UseMonadicSharpErrorLogging();
    //  (defined in MonadicSharp.Interop.AspNetCore)
    //
    //  Alternative: inline filter
    static RouteHandlerBuilder WithErrorLogging(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter(async (ctx, next) =>
        {
            var result = await next(ctx);
            // Log failures here without changing the response
            return result;
        });

    // ── 5e. ErrorType → ProblemDetails ────────────────────────────────────
    //
    //  .ToMinimalApiResult() creates RFC 7807 ProblemDetails responses:
    //  {
    //    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    //    "title": "Validation Error",
    //    "status": 400,
    //    "detail": "Product name is required",
    //    "extensions": { "code": "VALIDATION_ERROR", "field": "name" }
    //  }

    interface IAsyncProductService
    {
        Task<Result<ProductResponse>> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Result<ProductResponse>> CreateAsync(CreateProductRequest req, CancellationToken ct);
    }
}
