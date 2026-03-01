# MonadicApi

A REST API template built with **MonadicSharp** functional programming patterns.

## Features

- 🚦 **Railway-Oriented Programming** with `Result<T>` pattern
- 🛡️ **Type-Safe Error Handling** - No more exceptions for business logic
- 🎯 **Functional Validation** - Composable validation pipelines
- 🔄 **Monadic Operations** - Clean, chainable operations
- 📦 **CORS Support** - Ready for frontend integration
- 📚 **Swagger Documentation** - Interactive API documentation
- 🗃️ **Entity Framework Core** - With In-Memory database for demo

## Quick Start

```bash
# Run the application
dotnet run

# Browse to Swagger UI
# https://localhost:7000/swagger (or check console output for actual port)
```

## API Endpoints

### Users API

- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID  
- `POST /api/users` - Create new user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Example Usage

```bash
# Get all users
curl -X GET https://localhost:7000/api/users

# Create a user
curl -X POST https://localhost:7000/api/users \
  -H "Content-Type: application/json" \
  -d '{"name": "John Doe", "email": "john@example.com"}'

# Get user by ID
curl -X GET https://localhost:7000/api/users/1
```

## Functional Programming Patterns

### Result<T> Pattern

Instead of throwing exceptions, all operations return `Result<T>`:

```csharp
public async Task<Result<UserResponse>> GetUserByIdAsync(int id)
{
    return await ValidateUserId(id)
        .BindAsync(async validId => 
        {
            var user = await _context.Users.FindAsync(validId);
            return user != null 
                ? Result<User>.Success(user)
                : Result<User>.Failure(Error.Create($"User with ID {validId} not found"));
        })
        .Map(user => new UserResponse { ... });
}
```

### Validation Composition

Validations are composable and chainable:

```csharp
private static Result<CreateUserRequest> ValidateCreateUserRequest(CreateUserRequest request)
{
    return ValidateName(request.Name)
        .Bind(_ => ValidateEmail(request.Email))
        .Map(_ => request);
}
```

### Controller Pattern Matching

Controllers use pattern matching for clean response handling:

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id)
{
    var result = await _userService.GetUserByIdAsync(id);
    
    return result.Match(
        success: user => Ok(new { success = true, data = user }),
        failure: error => error.Code switch
        {
            "GENERAL_ERROR" when error.Message.Contains("not found") => 
                NotFound(new { success = false, error = new { code = error.Code, message = error.Message } }),
            _ => BadRequest(new { success = false, error = new { code = error.Code, message = error.Message } })
        }
    );
}
```

## Architecture

```
MonadicApi/
├── Controllers/          # API Controllers with Result<T> patterns
├── Services/            # Business logic with monadic operations  
├── Data/               # Entity Framework DbContext
├── Models/             # Domain models and DTOs
├── Middleware/         # Custom middleware for error handling
└── Program.cs          # Application startup
```

## Error Handling

The application uses a functional approach to error handling:

- ✅ **Business Logic Errors**: Returned as `Result<T>.Failure`
- ✅ **Validation Errors**: Composable validation with `Result<T>`
- ✅ **Not Found**: Handled through the Result pattern
- ✅ **Unhandled Exceptions**: Caught by middleware

## Dependencies

- **MonadicSharp** - Functional programming library
- **ASP.NET Core** - Web framework
- **Entity Framework Core** - ORM with In-Memory provider
- **Swashbuckle** - API documentation

## Learn More

- [MonadicSharp Documentation](https://github.com/Danny4897/MonadicSharp)
- [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/)
