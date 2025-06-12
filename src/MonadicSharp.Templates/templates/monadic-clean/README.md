# MonadicClean - Clean Architecture with Functional Programming

A **Clean Architecture** template built with **MonadicSharp** functional programming patterns, featuring CQRS, MediatR, and Railway-Oriented Programming.

## Architecture Overview

This template follows the **Clean Architecture** principles with functional programming patterns:

```
┌─────────────────────────────────────────────────────────────┐
│                        Presentation Layer                   │
│                     (MonadicClean.Api)                     │
│                   Controllers, Middleware                   │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                    Application Layer                        │
│                (MonadicClean.Application)                   │
│           Commands, Queries, DTOs, Handlers                 │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                     Domain Layer                            │
│                 (MonadicClean.Domain)                       │
│            Entities, Value Objects, Repositories            │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                  Infrastructure Layer                       │
│               (MonadicClean.Infrastructure)                 │
│               Data Access, External Services                │
└─────────────────────────────────────────────────────────────┘
```

## Key Features

- 🏗️ **Clean Architecture** - Clear separation of concerns
- 🚦 **Railway-Oriented Programming** - Functional error handling with `Result<T>`
- 🎯 **CQRS Pattern** - Command Query Responsibility Segregation
- 📨 **MediatR** - Mediator pattern for decoupled communication
- 🛡️ **Value Objects** - Type-safe domain modeling
- 🔄 **Monadic Operations** - Composable, chainable operations
- 📦 **Domain-Driven Design** - Rich domain models
- 🗃️ **Entity Framework Core** - Data persistence with Result<T> integration

## Quick Start

```bash
# Build the solution
dotnet build

# Run the API
dotnet run --project src/MonadicClean.Api

# Browse to Swagger UI
# https://localhost:7000/swagger (check console for actual port)
```

## Project Structure

### Domain Layer (`MonadicClean.Domain`)
Contains the core business logic and rules:

```
Domain/
├── Entities/           # Domain entities with behavior
│   └── User.cs
├── ValueObjects/       # Immutable value objects
│   └── Email.cs
├── Repositories/       # Repository interfaces
│   └── IUserRepository.cs
├── Services/          # Domain services
└── Common/            # Base classes and shared logic
    └── BaseEntity.cs
```

### Application Layer (`MonadicClean.Application`)
Contains use cases and orchestrates domain operations:

```
Application/
├── Users/
│   ├── Commands/      # Write operations (Create, Update, Delete)
│   │   ├── CreateUserCommand.cs
│   │   ├── UpdateUserCommand.cs
│   │   └── DeleteUserCommand.cs
│   └── Queries/       # Read operations (Get, List, Search)
│       ├── GetAllUsersQuery.cs
│       └── GetUserByIdQuery.cs
├── DTOs/              # Data Transfer Objects
│   └── UserDto.cs
└── Common/            # Shared interfaces and base classes
    └── Interfaces.cs
```

### Infrastructure Layer (`MonadicClean.Infrastructure`)
Contains implementations of external concerns:

```
Infrastructure/
├── Data/              # Database context and configurations
│   └── AppDbContext.cs
└── Repositories/      # Repository implementations
    └── UserRepository.cs
```

### Presentation Layer (`MonadicClean.Api`)
Contains API controllers and configuration:

```
Api/
├── Controllers/       # REST API controllers
│   └── UsersController.cs
├── Middleware/        # Custom middleware
└── Program.cs         # Application startup
```

## Functional Programming Patterns

### Domain Entities with Result<T>

```csharp
public class User : BaseEntity
{
    public static Result<User> Create(string name, string email)
    {
        return ValidateName(name)
            .Bind(_ => Email.Create(email))
            .Map(validEmail => new User(name, validEmail));
    }

    public Result<Unit> UpdateEmail(string newEmail)
    {
        return Email.Create(newEmail)
            .Map(validEmail =>
            {
                Email = validEmail;
                UpdateTimestamp();
                return Unit.Value;
            });
    }
}
```

### Value Objects with Validation

```csharp
public record Email
{
    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<Email>.Failure(Error.Create("Email cannot be empty"));

        if (!IsValidFormat(email))
            return Result<Email>.Failure(Error.Create("Invalid email format"));

        return Result<Email>.Success(new Email(email.ToLowerInvariant()));
    }
}
```

### CQRS with Result<T>

```csharp
// Command Handler
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserDto>
{
    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        return await _userRepository.EmailExistsAsync(request.Email)
            .Bind(emailExists => emailExists
                ? Result<Unit>.Failure(Error.Create($"User with email {request.Email} already exists"))
                : Result<Unit>.Success(Unit.Value))
            .Bind(_ => User.Create(request.Name, request.Email))
            .BindAsync(user => _userRepository.AddAsync(user))
            .Map(user => new UserDto { ... });
    }
}
```

### Repository with Functional Operations

```csharp
public async Task<Result<Option<User>>> GetByIdAsync(int id)
{
    try
    {
        var user = await _context.Users.FindAsync(id);
        return Result<Option<User>>.Success(Option<User>.From(user));
    }
    catch (Exception ex)
    {
        return Result<Option<User>>.Failure(Error.Create($"Failed to retrieve user {id}: {ex.Message}"));
    }
}
```

### Controllers with Pattern Matching

```csharp
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
{
    var result = await _mediator.Send(new CreateUserCommand(request.Name, request.Email));
    
    return result.Match(
        success: user => CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { success = true, data = user }),
        failure: error => error.Message.Contains("already exists") 
            ? Conflict(new { success = false, error = new { code = error.Code, message = error.Message } })
            : BadRequest(new { success = false, error = new { code = error.Code, message = error.Message } })
    );
}
```

## API Endpoints

- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create new user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

## Error Handling Philosophy

This template embraces **Railway-Oriented Programming**:

- ✅ **No Exceptions for Business Logic** - Use `Result<T>` instead
- ✅ **Composable Error Handling** - Chain operations safely
- ✅ **Type-Safe Error Propagation** - Errors are part of the type system
- ✅ **Explicit Error Handling** - Controllers explicitly handle success/failure cases
- ✅ **Rich Error Information** - Structured error objects with codes and messages

## Dependencies

- **MonadicSharp** - Functional programming library
- **MediatR** - Mediator pattern implementation
- **ASP.NET Core** - Web framework
- **Entity Framework Core** - ORM with In-Memory provider
- **Swashbuckle** - API documentation

## Learn More

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [MonadicSharp Documentation](https://github.com/Danny4897/MonadicSharp)
- [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Domain-Driven Design](https://en.wikipedia.org/wiki/Domain-driven_design)
