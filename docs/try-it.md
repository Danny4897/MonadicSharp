# Try it

Seleziona un esempio, leggi il codice, poi premi **▶ Run** per vedere l'output.

Nessun backend — tutto gira in questa pagina.

<div class="try-it-container">

<div class="try-tabs" id="tryTabs">
  <button class="try-tab active" data-tab="result">Result&lt;T&gt;</button>
  <button class="try-tab" data-tab="option">Option&lt;T&gt;</button>
  <button class="try-tab" data-tab="bind">Bind chain</button>
  <button class="try-tab" data-tab="errors">Error types</button>
  <button class="try-tab" data-tab="async">Async pipeline</button>
  <button class="try-tab" data-tab="cqrs">CQRS / DI</button>
</div>

<!-- ── Result<T> ── -->
<div class="try-panel active" id="tab-result">
<div class="try-description">

`Result<T>` è il tipo fondamentale di MonadicSharp. Ogni operazione restituisce **successo** o **fallimento** — mai entrambi, mai null, mai eccezioni implicite.

</div>
<div class="try-code">

```csharp
using MonadicSharp;

// Crea un successo
Result<int> ok = Result.Success(42);

// Crea un fallimento
Result<int> fail = Result.Failure<int>(
    Error.Validation("Il valore deve essere positivo"));

// Estrai il valore con Match — sei obbligato a gestire entrambi i casi
string output1 = ok.Match(
    onSuccess: v     => $"Valore: {v}",
    onFailure: error => $"Errore: {error.Message}");

string output2 = fail.Match(
    onSuccess: v     => $"Valore: {v}",
    onFailure: error => $"Errore: {error.Message}");

Console.WriteLine(output1);
Console.WriteLine(output2);
Console.WriteLine($"ok.IsSuccess   = {ok.IsSuccess}");
Console.WriteLine($"fail.IsSuccess = {fail.IsSuccess}");
```

</div>
<div class="try-run-bar">
  <button class="try-run-btn" onclick="runExample('result')">▶ Run</button>
</div>
<div class="try-output" id="output-result">
<div class="try-output-inner">

```
Valore: 42
Errore: Il valore deve essere positivo
ok.IsSuccess   = True
fail.IsSuccess = False
```

</div>
</div>
</div>

<!-- ── Option<T> ── -->
<div class="try-panel" id="tab-option">
<div class="try-description">

`Option<T>` sostituisce `null`. Un valore è `Some(x)` oppure `None` — il compilatore ti impedisce di ignorare l'assenza.

</div>
<div class="try-code">

```csharp
using MonadicSharp;

// Cerca un utente per ID — restituisce Option, non null
Option<string> FindUser(int id) =>
    id == 1 ? Option.Some("Alice") : Option.None<string>();

Option<string> found    = FindUser(1);
Option<string> notFound = FindUser(99);

// Map — trasforma il valore se presente, altrimenti None
Option<string> greeting = found.Map(name => $"Ciao, {name}!");

// GetValueOrDefault — valore di fallback esplicito
string name1 = found.GetValueOrDefault("Anonimo");
string name2 = notFound.GetValueOrDefault("Anonimo");

Console.WriteLine(greeting.GetValueOrDefault("Nessuno trovato"));
Console.WriteLine($"found    = {found}");
Console.WriteLine($"notFound = {notFound}");
Console.WriteLine($"name1    = {name1}");
Console.WriteLine($"name2    = {name2}");
```

</div>
<div class="try-run-bar">
  <button class="try-run-btn" onclick="runExample('option')">▶ Run</button>
</div>
<div class="try-output" id="output-option">
<div class="try-output-inner">

```
Ciao, Alice!
found    = Some(Alice)
notFound = None
name1    = Alice
name2    = Anonimo
```

</div>
</div>
</div>

<!-- ── Bind chain ── -->
<div class="try-panel" id="tab-bind">
<div class="try-description">

**Railway-Oriented Programming**: le operazioni si compongono con `Bind`. Se un passo fallisce, gli altri vengono saltati automaticamente — niente if-chain, niente try/catch.

</div>
<div class="try-code">

```csharp
using MonadicSharp;

Result<string> ValidateName(string name) =>
    string.IsNullOrWhiteSpace(name)
        ? Result.Failure<string>(Error.Validation("Nome obbligatorio"))
        : Result.Success(name.Trim());

Result<string> ValidateEmail(string email) =>
    email.Contains('@')
        ? Result.Success(email.ToLower())
        : Result.Failure<string>(Error.Validation("Email non valida"));

Result<string> CheckNotTaken(string email) =>
    email == "taken@example.com"
        ? Result.Failure<string>(Error.Conflict("Email già registrata"))
        : Result.Success(email);

// ─── Percorso felice ───
var happy = ValidateName("Alice")
    .Bind(_ => ValidateEmail("alice@example.com"))
    .Bind(CheckNotTaken)
    .Map(email => $"Utente creato con {email}");

// ─── Fallisce al primo step ───
var failName = ValidateName("")
    .Bind(_ => ValidateEmail("alice@example.com"))
    .Bind(CheckNotTaken);

// ─── Fallisce all'ultimo step ───
var failConflict = ValidateName("Bob")
    .Bind(_ => ValidateEmail("taken@example.com"))
    .Bind(CheckNotTaken);

Console.WriteLine(happy.Match(v => v, e => $"[{e.Type}] {e.Message}"));
Console.WriteLine(failName.Match(v => v, e => $"[{e.Type}] {e.Message}"));
Console.WriteLine(failConflict.Match(v => v, e => $"[{e.Type}] {e.Message}"));
```

</div>
<div class="try-run-bar">
  <button class="try-run-btn" onclick="runExample('bind')">▶ Run</button>
</div>
<div class="try-output" id="output-bind">
<div class="try-output-inner">

```
Utente creato con alice@example.com
[Validation] Nome obbligatorio
[Conflict] Email già registrata
```

</div>
</div>
</div>

<!-- ── Error types ── -->
<div class="try-panel" id="tab-errors">
<div class="try-description">

`Error` ha tipi semantici che si mappano direttamente sugli HTTP status code. Non servono eccezioni custom — il tipo dice già tutto.

</div>
<div class="try-code">

```csharp
using MonadicSharp;

// Tipi di errore predefiniti
var notFound   = Error.NotFound("Ordine 42 non trovato");
var validation = Error.Validation("Quantità deve essere > 0");
var conflict   = Error.Conflict("Email già registrata");
var forbidden  = Error.Forbidden("Accesso negato");
var unexpected = Error.Unexpected("Connessione al DB persa");

// Mappatura HTTP in un controller ASP.NET Core
IActionResult ToHttp(Error e) => e.Type switch
{
    ErrorType.NotFound   => NotFound(e.Message),      // 404
    ErrorType.Validation => BadRequest(e.Message),    // 400
    ErrorType.Conflict   => Conflict(e.Message),      // 409
    ErrorType.Forbidden  => Forbid(),                 // 403
    _                    => Problem(e.Message)         // 500
};

// Stampa tipo + messaggio
void Print(Error e) =>
    Console.WriteLine($"{e.Type,-12} → HTTP {HttpCode(e.Type),3}  |  {e.Message}");

int HttpCode(ErrorType t) => t switch {
    ErrorType.NotFound   => 404,
    ErrorType.Validation => 400,
    ErrorType.Conflict   => 409,
    ErrorType.Forbidden  => 403,
    _                    => 500
};

Print(notFound);
Print(validation);
Print(conflict);
Print(forbidden);
Print(unexpected);
```

</div>
<div class="try-run-bar">
  <button class="try-run-btn" onclick="runExample('errors')">▶ Run</button>
</div>
<div class="try-output" id="output-errors">
<div class="try-output-inner">

```
NotFound     → HTTP 404  |  Ordine 42 non trovato
Validation   → HTTP 400  |  Quantità deve essere > 0
Conflict     → HTTP 409  |  Email già registrata
Forbidden    → HTTP 403  |  Accesso negato
Unexpected   → HTTP 500  |  Connessione al DB persa
```

</div>
</div>
</div>

<!-- ── Async pipeline ── -->
<div class="try-panel" id="tab-async">
<div class="try-description">

`BindAsync` e `MapAsync` funzionano esattamente come le versioni sync, ma con `Task<Result<T>>`. La pipeline resta lineare anche con operazioni asincrone.

</div>
<div class="try-code">

```csharp
using MonadicSharp;

// Repository simulato — ogni metodo restituisce Task<Result<T>>
Task<Result<Guid>> FindUserAsync(string email) =>
    email == "alice@example.com"
        ? Task.FromResult(Result.Success(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001")))
        : Task.FromResult(Result.Failure<Guid>(Error.NotFound($"Utente {email} non trovato")));

Task<Result<decimal>> GetBalanceAsync(Guid userId) =>
    Task.FromResult(Result.Success(1_250.00m));

Task<Result<bool>> DebitAsync(Guid userId, decimal amount) =>
    amount <= 1_250.00m
        ? Task.FromResult(Result.Success(true))
        : Task.FromResult(Result.Failure<bool>(Error.Validation("Fondi insufficienti")));

// Pipeline: Find → GetBalance → Debit
// Se un passo fallisce, i successivi non vengono chiamati
var result = await FindUserAsync("alice@example.com")
    .BindAsync(userId  => GetBalanceAsync(userId))
    .BindAsync(balance => DebitAsync(Guid.Empty, balance - 500))
    .MapAsync(success  => "Pagamento completato");

Console.WriteLine(result.Match(msg => msg, e => $"[{e.Type}] {e.Message}"));

// Percorso di errore
var failed = await FindUserAsync("nobody@example.com")
    .BindAsync(userId  => GetBalanceAsync(userId))
    .BindAsync(balance => DebitAsync(Guid.Empty, balance));

Console.WriteLine(failed.Match(msg => msg, e => $"[{e.Type}] {e.Message}"));
```

</div>
<div class="try-run-bar">
  <button class="try-run-btn" onclick="runExample('async')">▶ Run</button>
</div>
<div class="try-output" id="output-async">
<div class="try-output-inner">

```
Pagamento completato
[NotFound] Utente nobody@example.com non trovato
```

</div>
</div>
</div>

<!-- ── CQRS / DI ── -->
<div class="try-panel" id="tab-cqrs">
<div class="try-description">

Con **MonadicSharp.DI** ogni handler restituisce `Result<T>` — niente eccezioni, niente try/catch nel controller. Il mediator dispatcha query e command.

</div>
<div class="try-code">

```csharp
using MonadicSharp;
using MonadicSharp.DI;

// ─── Query ───
public record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

public class GetOrderHandler(IOrderRepository orders)
    : IQueryHandler<GetOrderQuery, OrderDto>
{
    public async Task<Result<OrderDto>> HandleAsync(
        GetOrderQuery query, CancellationToken ct)
    {
        var order = await orders.FindAsync(query.OrderId, ct);

        // Map restituisce None→Failure automaticamente con il messaggio
        return order.Match(
            some: o    => Result.Success(new OrderDto(o.Id, o.Status, o.Total)),
            none: ()   => Result.Failure<OrderDto>(
                              Error.NotFound($"Ordine {query.OrderId} non trovato")));
    }
}

// ─── Controller ───
[HttpGet("{id}")]
public async Task<IActionResult> GetOrder(Guid id, IMediator mediator)
{
    var result = await mediator.QueryAsync(new GetOrderQuery(id));

    return result.Match(
        onSuccess: dto   => Ok(dto),
        onFailure: error => error.ToActionResult());
        //                  ↑ estensione che mappa ErrorType → HTTP status
}
```

</div>
<div class="try-run-bar">
  <button class="try-run-btn" onclick="runExample('cqrs')">▶ Run</button>
  <a class="try-docs-link" href="https://danny4897.github.io/MonadicSharp.DI/">Docs completi MonadicSharp.DI ↗</a>
</div>
<div class="try-output" id="output-cqrs">
<div class="try-output-inner">

```
// GET /orders/aaaaaaaa-0000-0000-0000-000000000001
HTTP 200 OK
{
  "id":     "aaaaaaaa-0000-0000-0000-000000000001",
  "status": "Shipped",
  "total":  149.90
}

// GET /orders/ffffffff-ffff-ffff-ffff-ffffffffffff
HTTP 404 Not Found
{
  "error": "Ordine ffffffff-ffff-ffff-ffff-ffffffffffff non trovato"
}
```

</div>
</div>
</div>

</div>

<style>
.try-it-container {
  margin: 2rem 0;
  border: 1px solid var(--vp-c-divider);
  border-radius: 12px;
  overflow: hidden;
}

.try-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 0;
  background: var(--vp-c-bg-soft);
  border-bottom: 1px solid var(--vp-c-divider);
  padding: 0.5rem 0.5rem 0;
}

.try-tab {
  padding: 0.45rem 1rem;
  font-size: 0.82rem;
  font-weight: 500;
  font-family: var(--vp-font-family-mono);
  border: 1px solid transparent;
  border-bottom: none;
  border-radius: 6px 6px 0 0;
  background: transparent;
  color: var(--vp-c-text-2);
  cursor: pointer;
  transition: color 0.15s, background 0.15s, border-color 0.15s;
  margin-bottom: -1px;
}

.try-tab:hover {
  color: var(--vp-c-brand-1);
  background: var(--vp-c-bg-mute);
}

.try-tab.active {
  color: var(--vp-c-brand-1);
  background: var(--vp-c-bg);
  border-color: var(--vp-c-divider);
  border-bottom-color: var(--vp-c-bg);
}

.try-panel {
  display: none;
  padding: 1.25rem 1.5rem 1.5rem;
}

.try-panel.active {
  display: block;
}

.try-description {
  margin-bottom: 1rem;
  font-size: 0.92rem;
  color: var(--vp-c-text-2);
  line-height: 1.6;
}

.try-description code {
  background: var(--vp-c-brand-soft) !important;
  color: var(--vp-c-brand-1) !important;
  border-radius: 4px;
  padding: 0.1em 0.3em;
  font-size: 0.88em;
}

.try-code {
  margin-bottom: 0;
}

.try-run-bar {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-top: 0.75rem;
  margin-bottom: 0.25rem;
}

.try-run-btn {
  padding: 0.4rem 1.2rem;
  font-size: 0.85rem;
  font-weight: 600;
  font-family: var(--vp-font-family-mono);
  background: var(--vp-c-brand-1);
  color: #fff;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: background 0.15s, transform 0.1s;
  letter-spacing: 0.02em;
}

.try-run-btn:hover {
  background: var(--vp-c-brand-2);
  transform: translateY(-1px);
}

.try-run-btn:active {
  transform: translateY(0);
}

.try-docs-link {
  font-size: 0.82rem;
  color: var(--vp-c-brand-1);
  text-decoration: none;
}

.try-docs-link:hover {
  text-decoration: underline;
}

.try-output {
  max-height: 0;
  overflow: hidden;
  transition: max-height 0.35s ease, opacity 0.25s ease;
  opacity: 0;
}

.try-output.visible {
  max-height: 600px;
  opacity: 1;
}

.try-output-inner {
  margin-top: 0.75rem;
}

@media (max-width: 600px) {
  .try-tab {
    font-size: 0.75rem;
    padding: 0.4rem 0.65rem;
  }
  .try-panel {
    padding: 1rem;
  }
}
</style>

<script>
(function () {
  function activate(tabId) {
    document.querySelectorAll('.try-tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.try-panel').forEach(p => p.classList.remove('active'));
    const btn = document.querySelector(`.try-tab[data-tab="${tabId}"]`);
    const panel = document.getElementById('tab-' + tabId);
    if (btn)   btn.classList.add('active');
    if (panel) panel.classList.add('active');
    // hide output when switching tab
    document.querySelectorAll('.try-output').forEach(o => o.classList.remove('visible'));
  }

  document.querySelectorAll('.try-tab').forEach(btn => {
    btn.addEventListener('click', () => activate(btn.dataset.tab));
  });

  window.runExample = function (id) {
    const out = document.getElementById('output-' + id);
    if (!out) return;
    out.classList.toggle('visible');
  };
})();
</script>

---

## Prossimi passi

- [Getting Started](/getting-started) — aggiungi MonadicSharp al tuo progetto in 5 minuti
- [Core Types](/core/result) — documentazione completa di `Result<T>`, `Option<T>`, `Error`
- [Async Pipelines](/pipelines) — `BindAsync`, `MapAsync`, `TapAsync` in dettaglio
- [Ecosystem](/ecosystem/) — pacchetti aggiuntivi per DI, Azure, AI, Recovery
