// Examples — documentation only, not compiled.
// ReSharper disable All
#pragma warning disable CS0168, CS8019, CS1998
using System.Net.Http.Json;
using System.Text.Json;
using MonadicSharp;
using MonadicSharp.Extensions;
using MonadicSharp.Interop;

namespace MonadicSharp.Examples;

// ============================================================
// 07 — HTTP CLIENT INTEROP  ·  MonadicSharp.Interop
//      Wrap HttpClient calls in Result<T> pipelines
//      without boilerplate try/catch
// ============================================================
//
//  Extension methods on HttpClient:
//    GetAsResult<T>(url)                       — GET → Result<T>
//    PostAsResult<TReq, TResp>(url, payload)   — POST → Result<TResp>
//
//  Extension methods on HttpResponseMessage:
//    ToResult()                  — status check → Result<Unit>
//    ReadAsResult<T>()           — deserialise body → Result<T>
//    IsMonadicSuccess            — bool property (.NET 10+ extension member)
//
//  HttpErrorMapper:
//    400 → ErrorType.Validation
//    401/403 → ErrorType.Forbidden
//    404 → ErrorType.NotFound
//    409 → ErrorType.Conflict
//    5xx → ErrorType.Failure
// ============================================================

static class HttpInteropExamples
{
    record WeatherForecast(string City, double TempC, string Condition);
    record GeocodingRequest(string Address);
    record GeocodingResponse(double Lat, double Lon);
    record SubmitOrderRequest(string ProductId, int Quantity);
    record OrderConfirmation(string OrderId, DateTimeOffset EstimatedDelivery);

    // ── 7a. Simple GET with typed deserialization ──────────────────────────

    static async Task<Result<WeatherForecast>> GetWeatherAsync(HttpClient client, string city) =>
        await client.GetAsResult<WeatherForecast>($"/weather/{Uri.EscapeDataString(city)}");

    // ── 7b. POST with request/response types ──────────────────────────────

    static async Task<Result<OrderConfirmation>> PlaceOrderAsync(
        HttpClient client, SubmitOrderRequest order) =>
        await client.PostAsResult<SubmitOrderRequest, OrderConfirmation>("/orders", order);

    // ── 7c. Pipeline chaining HTTP calls ──────────────────────────────────

    static async Task<Result<string>> GetWeatherForAddress(
        HttpClient geocodeClient, HttpClient weatherClient, string address)
    {
        // Step 1: geocode the address
        var coords = await geocodeClient.PostAsResult<GeocodingRequest, GeocodingResponse>(
            "/geocode", new(address));

        // Step 2: fetch weather using coordinates (only if geocode succeeded)
        return await coords
            .BindAsync(c => weatherClient.GetAsResult<WeatherForecast>(
                $"/weather?lat={c.Lat}&lon={c.Lon}"))
            .MapAsync(w => $"{w.City}: {w.TempC}°C, {w.Condition}");
    }

    // ── 7d. Direct HttpResponseMessage extension methods ──────────────────

    static async Task<Result<OrderConfirmation>> PlaceOrderRaw(HttpClient client, SubmitOrderRequest order)
    {
        var response = await client.PostAsJsonAsync("/orders", order);

        // ToResult() checks status, ReadAsResult<T>() deserialises body
        return await response.ReadAsResult<OrderConfirmation>();
        // Non-2xx → typed error (e.g. 409 → ErrorType.Conflict)
        // Null body → Failure("Response body deserialized to null")
    }

    // ── 7e. Checking status without reading the body ───────────────────────

    static async Task<Result<Unit>> PingHealthEndpoint(HttpClient client)
    {
        var response = await client.GetAsync("/health");
        return response.ToResult(); // Result<Unit>: success if 2xx
    }

    // ── 7f. Resilient client with retry ───────────────────────────────────

    class WeatherService
    {
        private readonly HttpClient _client;

        public WeatherService(HttpClient client) => _client = client;

        public async Task<Result<WeatherForecast>> GetWithRetryAsync(string city, int maxAttempts = 3)
        {
            Result<WeatherForecast> result = Result<WeatherForecast>.Failure("Not started");
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                result = await _client.GetAsResult<WeatherForecast>($"/weather/{city}");
                if (result.IsSuccess || result.Error.Type == ErrorType.NotFound)
                    break;  // don't retry client errors or not-found

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // exponential backoff
            }
            return result;
        }

        public Task<string> GetDisplayAsync(string city) =>
            GetWithRetryAsync(city).ContinueWith(t =>
                t.Result.Match(
                    w => $"{w.City}: {w.TempC:F1}°C",
                    e => $"[{e.Type}] Could not get weather: {e.Message}"
                ));
    }

    // ── 7g. Named HttpClient registered in DI ─────────────────────────────

    /*  Program.cs:

        builder.Services.AddHttpClient<WeatherService>(c =>
        {
            c.BaseAddress = new Uri("https://api.weather.example.com");
            c.Timeout     = TimeSpan.FromSeconds(10);
        });

        // Usage in endpoint:
        app.MapGet("/weather/{city}", async (string city, WeatherService svc) =>
            (await svc.GetWithRetryAsync(city)).ToOkResult());
    */
}
