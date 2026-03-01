# MonadicSharp Templates

A collection of .NET project templates that leverage **MonadicSharp** functional programming patterns for building robust applications with Railway-Oriented Programming.

## Available Templates

### 🌐 Monadic API (`monadic-api`)
A REST API template with MonadicSharp functional programming patterns.

**Features:**
- Result<T> pattern for error handling
- Functional service architecture
- CORS support
- Swagger documentation
- Entity Framework Core integration
- Custom middleware for Result<T> error handling

**Usage:**
```bash
dotnet new install MonadicSharp.Templates
dotnet new monadic-api -n MyApi
cd MyApi
dotnet run
```

### 🏗️ Monadic Clean Architecture (`monadic-clean`)
A comprehensive Clean Architecture template with functional programming patterns.

**Features:**
- Clean Architecture with 4 layers
- CQRS pattern with MediatR
- Railway-Oriented Programming
- Domain-Driven Design
- Value Objects with validation
- Functional repository pattern
- Rich domain models

**Usage:**
```bash
dotnet new install MonadicSharp.Templates
dotnet new monadic-clean -n MyCleanApp
cd MyCleanApp
dotnet build
dotnet run --project src/MyCleanApp.Api
```

## Installation

### From NuGet (Recommended)
```bash
# Install the templates
dotnet new install MonadicSharp.Templates

# List available templates
dotnet new list MonadicSharp

# Create a new project
dotnet new monadic-api -n MyApiProject
dotnet new monadic-clean -n MyCleanProject
```

### From Source
```bash
# Clone the repository
git clone https://github.com/Danny4897/MonadicSharp.git
cd MonadicSharp/src/MonadicSharp.Templates

# Install templates locally
dotnet new install .

# Create projects
dotnet new monadic-api -n MyApiProject
dotnet new monadic-clean -n MyCleanProject
```

## Template Comparison

| Feature | monadic-api | monadic-clean |
|---------|-------------|---------------|
| Architecture | Simple layered | Clean Architecture |
| Complexity | Beginner | Intermediate |
| CQRS | ❌ | ✅ |
| MediatR | ❌ | ✅ |
| DDD | ❌ | ✅ |
| Value Objects | ❌ | ✅ |
| Result<T> Pattern | ✅ | ✅ |
| Entity Framework | ✅ | ✅ |
| Swagger | ✅ | ✅ |
| Setup Time | 5 minutes | 10 minutes |

## What You Get

Both templates provide:

- **Functional Error Handling**: No more exceptions for business logic
- **Type Safety**: Leverage C#'s type system for robust code
- **Railway-Oriented Programming**: Elegant error handling patterns
- **MonadicSharp Integration**: Out-of-the-box functional programming
- **Production Ready**: Structured, maintainable code
- **Documentation**: Comprehensive README and code comments

## Example Code

### Result<T> Pattern
```csharp
public async Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request)
{
    return await ValidateCreateUserRequest(request)
        .BindAsync(async validRequest =>
        {
            var user = new User
            {
                Name = validRequest.Name,
                Email = validRequest.Email,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Result<UserResponse>.Success(MapToUserResponse(user));
        });
}
```

### Controller with Pattern Matching
```csharp
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    var result = await _userService.CreateUserAsync(request);
    
    return result.Match(
        success: user => CreatedAtAction(nameof(GetUser), new { id = user.Id }, 
            new { success = true, data = user }),
        failure: error => error.Message.Contains("already exists") 
            ? Conflict(new { success = false, error = new { code = error.Code, message = error.Message } })
            : BadRequest(new { success = false, error = new { code = error.Code, message = error.Message } })
    );
}
```

## Getting Started

1. **Install MonadicSharp templates**:
   ```bash
   dotnet new install MonadicSharp.Templates
   ```

2. **Choose your template**:
   - For simple APIs: `dotnet new monadic-api -n MyApi`
   - For complex applications: `dotnet new monadic-clean -n MyApp`

3. **Run your application**:
   ```bash
   cd MyApi  # or MyApp
   dotnet run
   ```

4. **Explore the patterns**:
   - Check the controllers for Result<T> usage
   - Look at validation with functional composition
   - See error handling with pattern matching

## Resources

- [MonadicSharp Library](https://github.com/Danny4897/MonadicSharp)
- [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License.

---

Made with ❤️ by [Danny4897](https://github.com/Danny4897)
