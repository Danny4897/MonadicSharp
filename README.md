# MonadicSharp

[![NuGet Version](https://img.shields.io/nuget/v/MonadicSharp.svg)](https://www.nuget.org/packages/MonadicSharp/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MonadicSharp.svg)](https://www.nuget.org/packages/MonadicSharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub](https://img.shields.io/badge/GitHub-Danny4897-blue.svg)](https://github.com/Danny4897)
[![NuGet](https://img.shields.io/badge/NuGet-Klexir-orange.svg)](https://www.nuget.org/profiles/Klexir)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/Language-C%23-green.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)

> 🏆 **Created by [Danny4897](https://github.com/Danny4897) (aka [Klexir](https://www.nuget.org/profiles/Klexir) on NuGet)** | 📦 **Available on [NuGet.org](https://www.nuget.org/packages/MonadicSharp/)**

A modern functional programming library for C# featuring **Result<T>**, **Option<T>**, **Railway-Oriented Programming**, and fluent pipeline composition. Build robust applications with elegant error handling and monadic patterns.

## ✨ Features

- 🚦 **Result<T>** - Railway-oriented programming for error handling
- 🎯 **Option<T>** - Null-safe operations and optional values
- 🔄 **Fluent Pipelines** - Chain operations with elegant syntax
- ⚡ **Async Support** - Full async/await compatibility
- 🛡️ **Type Safety** - Leverage C#'s type system for bulletproof code
- 📦 **Zero Dependencies** - Lightweight and self-contained

## 🚀 Quick Start

### Installation

```bash
dotnet add package MonadicSharp
```

### Basic Usage

```csharp
using MonadicSharp;
using MonadicSharp.Extensions;

// Result<T> for error handling
var result = Result.Success(42)
    .Map(x => x * 2)
    .Bind(x => x > 50 ? Result.Success(x) : Result.Failure("Too small"))
    .Map(x => $"Final value: {x}");

result.Match(
    onSuccess: value => Console.WriteLine(value),
    onFailure: error => Console.WriteLine($"Error: {error}")
);

// Option<T> for null-safe operations
var user = GetUser()
    .Map(u => u.Email)
    .Filter(email => email.Contains("@"))
    .GetValueOrDefault("no-email@example.com");
```

## 🔧 Core Types

### Result<T>

Railway-oriented programming for elegant error handling:

```csharp
// Success path
var success = Result.Success("Hello World");

// Failure path
var failure = Result.Failure<string>("Something went wrong");

// Chaining operations
var result = GetUser(id)
    .Bind(ValidateUser)
    .Bind(SaveUser)
    .Map(u => u.Id);
```

### Option<T>

Null-safe operations without null reference exceptions:

```csharp
// Some value
var some = Option.Some(42);

// No value
var none = Option.None<int>();

// Safe operations
var result = GetUser()
    .Map(u => u.Name)
    .Filter(name => name.Length > 0)
    .GetValueOrDefault("Anonymous");
```

## 🔄 Pipeline Composition

Build complex data processing pipelines:

```csharp
var pipeline = Pipeline
    .Start<string>()
    .Then(ParseNumber)
    .Then(ValidateRange)
    .Then(FormatOutput);

var result = await pipeline.ExecuteAsync("42");
```

## 🎯 Error Handling Patterns

### Traditional Approach (Problematic)
```csharp
public User GetUser(int id)
{
    var user = database.Find(id);
    if (user == null) 
        throw new UserNotFoundException();
    
    if (!user.IsActive)
        throw new UserInactiveException();
        
    return user;
}
```

### MonadicSharp Approach (Clean)
```csharp
public Result<User> GetUser(int id)
{
    return database.Find(id)
        .ToResult("User not found")
        .Bind(ValidateUserActive);
}

private Result<User> ValidateUserActive(User user)
{
    return user.IsActive 
        ? Result.Success(user)
        : Result.Failure("User is inactive");
}
```

## 📖 API Reference

### Result<T> Methods

- `Success<T>(T value)` - Create a successful result
- `Failure<T>(string error)` - Create a failed result
- `Map<TResult>(Func<T, TResult> func)` - Transform the success value
- `Bind<TResult>(Func<T, Result<TResult>> func)` - Chain operations
- `Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)` - Pattern match
- `IsSuccess` - Check if result is successful
- `IsFailure` - Check if result has failed
- `Value` - Get the success value (throws if failed)
- `Error` - Get the error message (empty if successful)

### Option<T> Methods

- `Some<T>(T value)` - Create an option with value
- `None<T>()` - Create an empty option
- `Map<TResult>(Func<T, TResult> func)` - Transform the value if present
- `Bind<TResult>(Func<T, Option<TResult>> func)` - Chain operations
- `Filter(Func<T, bool> predicate)` - Filter based on condition
- `Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone)` - Pattern match
- `GetValueOrDefault(T defaultValue)` - Get value or default
- `HasValue` - Check if option has a value
- `Value` - Get the value (throws if empty)

### Extension Methods

```csharp
// Convert nullable to Option
string? nullable = GetNullableString();
var option = nullable.ToOption();

// Convert to Result
var result = option.ToResult("Value was null");

// Async operations
var asyncResult = await GetUserAsync(id)
    .MapAsync(user => user.Email)
    .BindAsync(ValidateEmailAsync);
```

## 🏗️ Requirements

- .NET 8.0 or later
- C# 10.0 or later

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📚 Learn More

- [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Functional Programming in C#](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/functional-programming/)
- [Monads Explained](https://ericlippert.com/category/monads/)

---

Made with ❤️ by [Danny4897](https://github.com/Danny4897)

# FunctionalSharp

Una libreria .NET che fornisce implementazioni di tipi monadici e funzionali per C#.

## Caratteristiche

- Implementazione di `Option<T>` per gestire valori opzionali
- Implementazione di `Result<T>` per gestire operazioni che possono fallire
- Supporto per operazioni asincrone con `Task`
- Integrazione con Entity Framework Core
- Pipeline funzionali concise

## Installazione

```bash
dotnet add package MonadicSharp
```

## Esempi di Utilizzo

### Gestione di Valori Opzionali

```csharp
// Creazione di un Option
var someValue = Option<int>.Some(42);
var noneValue = Option<int>.None;

// Pattern matching
var result = someValue.Match(
    some: value => $"Il valore è {value}",
    none: () => "Nessun valore"
);

// Map e Bind
var doubled = someValue.Map(x => x * 2);
var maybeString = someValue.Bind(x => x > 0 ? Option<string>.Some(x.ToString()) : Option<string>.None);
```

### Gestione di Operazioni che Possono Fallire

```csharp
// Creazione di un Result
var success = Result<int>.Success(42);
var failure = Result<int>.Failure(Error.Create("Operazione fallita"));

// Pattern matching
var result = success.Match(
    success: value => $"Operazione riuscita: {value}",
    failure: error => $"Errore: {error.Message}"
);

// Map e Bind
var doubled = success.Map(x => x * 2);
var maybeString = success.Bind(x => x > 0 ? Result<string>.Success(x.ToString()) : Result<string>.Failure(Error.Create("Valore non valido")));
```

### Pipeline Funzionali Con Entity Framework

```csharp
// Creazione di un utente con validazione
public async Task<Result<User>> CreateUserAsync(UserDto userDto)
{
    return await Result<UserDto>.Success(userDto)
        .Map(ValidateUserDto)
        .Bind(MapToUser)
        .Bind(user => _userRepository.Users.AddAsync(user))
        .BindAsync(SaveChangesAsync)
        .Success(user => Result<User>.Success(user))
        .Failure(error => Result<User>.Failure(error));
}

// Recupero di un utente con gestione errori
public async Task<Result<User>> GetUserByIdAsync(int id)
{
    return await Result<int>.Success(id)
        .Bind(id => _userRepository.Users.FindAsync(id))
        .BindAsync(user => user.ToResult("Utente non trovato"))
        .Success(user => Result<User>.Success(user))
        .Failure(error => Result<User>.Failure(error));
}

// Aggiornamento di un utente con validazione
public async Task<Result<User>> UpdateUserAsync(int id, UserDto userDto)
{
    return await Result<(int id, UserDto dto)>.Success((id, userDto))
        .Map(tuple => (tuple.id, ValidateUserDto(tuple.dto)))
        .Bind(tuple => _userRepository.Users.FindAsync(tuple.id)
            .BindAsync(user => user.ToResult("Utente non trovato"))
            .Bind(user => MapToUser(tuple.dto).Map(dto => (user, dto))))
        .Bind(tuple => _userRepository.Users.Update(tuple.user))
        .BindAsync(SaveChangesAsync)
        .Success(user => Result<User>.Success(user))
        .Failure(error => Result<User>.Failure(error));
}
```

### Operazioni Asincrone

```csharp
// Combinazione di operazioni asincrone
public async Task<Result<User>> GetUserWithDetailsAsync(int userId)
{
    return await Result<int>.Success(userId)
        .BindAsync(async id => 
        {
            var user = await _userRepository.Users.FindAsync(id);
            var roles = await _userRepository.Roles.WhereAsync(r => r.UserId == id);
            return (user, roles);
        })
        .BindAsync(async tuple => 
        {
            var user = await tuple.user.ToResult("Utente non trovato");
            var roles = await tuple.roles;
            return Result<User>.Success(user with { Roles = roles });
        });
}
```

## Contribuire

Le contribuzioni sono benvenute! Per favore, apri una issue per discutere i cambiamenti che vorresti fare.

## Licenza

Questo progetto è licenziato sotto la licenza MIT - vedi il file [LICENSE](LICENSE) per i dettagli.
