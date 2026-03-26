# Templates

`MonadicSharp.Templates` is a .NET template package that scaffolds production-ready projects with MonadicSharp pre-configured.

## Installation

```bash
dotnet new install MonadicSharp.Templates
```

Verify installation:

```bash
dotnet new list | grep monadic
```

## monadic-api

A Minimal API project with:
- `Result<T>` on all service methods
- EF Core 8 + `AppDbContext`
- `UserService` that returns `Result<User>` — ready to extend
- `Match` in endpoints to map errors to HTTP status codes

```bash
dotnet new monadic-api -n MyApi
cd MyApi
dotnet run
```

### Project structure

```
MyApi/
├── Data/
│   └── AppDbContext.cs
├── Models/
│   └── User.cs
├── Services/
│   └── UserService.cs       ← returns Result<T>
└── Program.cs               ← endpoints use Match
```

### Example endpoint (from the template)

```csharp
app.MapPost("/users", async (CreateUserRequest req, UserService svc) =>
{
    var result = await svc.CreateAsync(req);
    return result.Match(
        onSuccess: user  => Results.Created($"/users/{user.Id}", user),
        onFailure: error => error.Type switch
        {
            ErrorType.Validation => Results.BadRequest(error.Message),
            ErrorType.Conflict   => Results.Conflict(error.Message),
            _                    => Results.Problem(error.Message)
        });
});
```

## monadic-clean

A Clean Architecture solution with CQRS:
- Domain, Application, Infrastructure, API layers
- Command and Query handlers that return `Result<T>`
- `IUserRepository` with Result-aware EF Core implementation
- `Email` value object with validation via `Result<T>`

```bash
dotnet new monadic-clean -n MyApp
cd MyApp
dotnet build
```

### Solution structure

```
MyApp/
├── src/
│   ├── MyApp.Domain/
│   │   ├── Entities/User.cs
│   │   ├── Repositories/IUserRepository.cs
│   │   └── ValueObjects/Email.cs         ← Result<Email> factory
│   ├── MyApp.Application/
│   │   ├── Users/Commands/               ← CreateUser, UpdateUser, DeleteUser
│   │   └── Users/Queries/                ← GetUserById, GetAllUsers
│   ├── MyApp.Infrastructure/
│   │   ├── Data/AppDbContext.cs
│   │   └── Repositories/UserRepository.cs
│   └── MyApp.Api/
│       ├── Controllers/UsersController.cs ← Match at the boundary
│       └── Program.cs
└── MyApp.sln
```

### Example command handler

```csharp
public async Task<Result<UserDto>> Handle(CreateUserCommand command)
{
    var email = Email.Create(command.Email);  // Result<Email>
    if (email.IsFailure) return email.Error;

    return await _repository
        .ExistsAsync(email.Value)
        .Bind(exists => exists
            ? Result<User>.Failure(Error.Conflict("Email already registered"))
            : Result<User>.Success(new User(command.Name, email.Value)))
        .Bind(user => _repository.AddAsync(user))
        .Map(user  => new UserDto(user));
}
```

## AI assistant setup

Both templates include:
- `.cursorrules` — Cursor IDE generates MonadicSharp code by default
- `.github/copilot-instructions.md` — GitHub Copilot follows MonadicSharp patterns
- `claude-system-prompt.md` — Claude system prompt for MonadicSharp-first suggestions

No configuration needed — open the project and the AI assistant is already guided.

## Uninstall

```bash
dotnet new uninstall MonadicSharp.Templates
```
