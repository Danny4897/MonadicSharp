# /migrate

Auto-migrates green-code violations in the current file. Converts bare `throw` statements to `Result<T>`, nullable returns to `Option<T>`, and other violations to their MonadicSharp equivalents.

## What it does

1. Runs the same analysis as `/forge-analyze` on the current file.
2. For each auto-migratable violation, computes a code transformation.
3. Shows a preview diff of all changes before applying anything.
4. Applies changes only when you confirm (or when `--dry-run` is omitted and you have set `autoApply` in `.monadicleaf.json`).

Not every violation is auto-migratable. Violations that require understanding business logic (e.g., choosing an appropriate error type) are flagged for manual review.

## Usage

```
/migrate
/migrate --dry-run
/migrate --rule GC001
/migrate --file src/Services/OrderService.cs
```

## Options

| Option | Default | Description |
|---|---|---|
| `--dry-run` | off | Preview changes without writing to disk |
| `--rule` | all auto-migratable rules | Limit migration to a single rule code |
| `--file` | current file | Override the file to migrate |

## Preview output (before confirmation)

```
File: src/Services/OrderService.cs
3 changes ready to apply

──────────────── GC001 — line 42 ────────────────
 Before:
   throw new InvalidOperationException("Order not found");

 After:
   return Result.Fail<Order>(new OrderError.NotFound(orderId));

──────────────── GC003 — line 67 ────────────────
 Before:
   public User? GetByEmail(string email)
   {
       return _db.Users.FirstOrDefault(u => u.Email == email);
   }

 After:
   public Option<User> GetByEmail(string email)
   {
       var user = _db.Users.FirstOrDefault(u => u.Email == email);
       return user is null ? Option.None<User>() : Option.Some(user);
   }

──────────────── GC001 — line 89 ────────────────
 Before:
   catch (DbException ex)
   {
       throw new RepositoryException("Database error", ex);
   }

 After:
   catch (DbException ex)
   {
       return Result.Fail<Unit>(new PersistenceError.DatabaseUnavailable(ex.Message));
   }

Apply these changes? [y/N]
```

## Example: before and after GC001

**Before** — bare `throw` propagates as an untyped exception:

```csharp
public Order GetById(Guid orderId)
{
    var order = _db.Orders.Find(orderId);
    if (order is null)
        throw new InvalidOperationException($"Order {orderId} not found");

    return order;
}
```

**After** — error is a value, return type is explicit:

```csharp
public Result<Order> GetById(Guid orderId)
{
    var order = _db.Orders.Find(orderId);
    if (order is null)
        return Result.Fail<Order>(new OrderError.NotFound(orderId));

    return Result.Ok(order);
}
```

## Auto-migratable rules

| Rule | Migration |
|---|---|
| GC001 | `throw` → `return Result.Fail<T>(...)` |
| GC003 | `T?` return → `Option<T>` return |
| GC004 | `bool` failable return → `Result<T>` |
| GC005 | Empty `catch {}` → explicit error result |
| GC008 | Nullable reference type field → `Option<T>` field |

Rules GC002, GC006, GC007, GC009, and GC010 require context-specific decisions and are not auto-migrated. `/migrate` will list them and link to the manual migration guide for each.

## Configuration

```json
{
  "migration": {
    "autoApply": false,
    "defaultErrorNamespace": "MyApp.Errors",
    "preserveOriginalComments": true
  }
}
```

`autoApply: true` skips the confirmation prompt and writes changes immediately. Use with caution.
