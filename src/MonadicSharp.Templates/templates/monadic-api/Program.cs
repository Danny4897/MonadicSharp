using MonadicSharp;
using MonadicSharp.Extensions;
using MonadicApi.Data;
using MonadicApi.Services;
using MonadicApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== MonadicSharp Demo Application ===");
Console.WriteLine();

// Create host builder for dependency injection
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("MonadicApiDb"));
        services.AddScoped<IUserService, UserService>();
        services.AddLogging();
    })
    .Build();

// Run the demo
using var scope = host.Services.CreateScope();
var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

await RunDemo(userService, logger);

static async Task RunDemo(IUserService userService, ILogger logger)
{
    logger.LogInformation("Starting MonadicSharp demonstration...");

    // Seed some initial data
    await SeedInitialData(userService, logger);

    // Demonstrate Result<T> pattern with success and failure cases
    await DemonstrateResultPattern(userService, logger);

    // Demonstrate functional composition
    await DemonstrateFunctionalComposition(userService, logger);

    Console.WriteLine();
    Console.WriteLine("=== Demo completed! Press any key to exit ===");
    Console.ReadKey();
}

static async Task SeedInitialData(IUserService userService, ILogger logger)
{
    Console.WriteLine("🌱 Seeding initial data...");

    var users = new[]
    {
        new CreateUserRequest { Name = "John Doe", Email = "john@example.com" },
        new CreateUserRequest { Name = "Jane Smith", Email = "jane@example.com" },
        new CreateUserRequest { Name = "Bob Johnson", Email = "bob@example.com" }
    };

    foreach (var request in users)
    {
        var result = await userService.CreateUserAsync(request);
        result.Match(
            onSuccess: user => Console.WriteLine($"✅ Created user: {user.Name} ({user.Email})"),
            onFailure: error => Console.WriteLine($"❌ Failed to create user: {error.Message}")
        );
    }
    Console.WriteLine();
}

static async Task DemonstrateResultPattern(IUserService userService, ILogger logger)
{
    Console.WriteLine("🎯 Demonstrating Result<T> Pattern:");
    Console.WriteLine();

    // Success case
    Console.WriteLine("📋 Getting all users:");
    var allUsersResult = await userService.GetAllUsersAsync();
    allUsersResult.Match(
        onSuccess: users =>
        {
            Console.WriteLine($"✅ Found {users.Count()} users:");
            foreach (var user in users)
            {
                var status = user.IsActive ? "Active" : "Inactive";
                Console.WriteLine($"   - {user.Name} ({user.Email}) - {status}");
            }
        },
        onFailure: error => Console.WriteLine($"❌ Error: {error.Message}")
    );
    Console.WriteLine();

    // Success case - get specific user
    Console.WriteLine("🔍 Getting user by ID (should succeed):");
    var userResult = await userService.GetUserByIdAsync(1);
    userResult.Match(
        onSuccess: user => Console.WriteLine($"✅ Found user: {user.Name} ({user.Email})"),
        onFailure: error => Console.WriteLine($"❌ Error: {error.Message}")
    );
    Console.WriteLine();

    // Failure case - get non-existent user
    Console.WriteLine("🔍 Getting user by ID (should fail):");
    var notFoundResult = await userService.GetUserByIdAsync(999);
    notFoundResult.Match(
        onSuccess: user => Console.WriteLine($"✅ Found user: {user.Name}"),
        onFailure: error => Console.WriteLine($"❌ Expected error: {error.Message}")
    );
    Console.WriteLine();

    // Failure case - invalid data
    Console.WriteLine("📝 Creating user with invalid data (should fail):");
    var invalidResult = await userService.CreateUserAsync(new CreateUserRequest
    {
        Name = "", // Empty name should fail validation
        Email = "invalid-email"
    });
    invalidResult.Match(
        onSuccess: user => Console.WriteLine($"✅ Created user: {user.Name}"),
        onFailure: error => Console.WriteLine($"❌ Expected validation error: {error.Message}")
    );
    Console.WriteLine();
}

static async Task DemonstrateFunctionalComposition(IUserService userService, ILogger logger)
{
    Console.WriteLine("🔗 Demonstrating Functional Composition:");
    Console.WriteLine();

    // Chain operations using Bind and Map
    Console.WriteLine("🔄 Chaining operations: Get user → Update → Display result");

    var chainResult = await userService.GetUserByIdAsync(1)
        .BindAsync(async user =>
        {
            Console.WriteLine($"   📦 Retrieved: {user.Name}");
            return await userService.UpdateUserAsync(user.Id, new UpdateUserRequest
            {
                Name = user.Name + " (Updated)",
                Email = user.Email,
                IsActive = !user.IsActive
            });
        })
        .Map(updatedUser =>
        {
            Console.WriteLine($"   ✨ Updated: {updatedUser.Name} - Active: {updatedUser.IsActive}");
            return updatedUser;
        });

    chainResult.Match(
        onSuccess: user => Console.WriteLine($"✅ Chain completed successfully for: {user.Name}"),
        onFailure: error => Console.WriteLine($"❌ Chain failed: {error.Message}")
    );
    Console.WriteLine();

    // Demonstrate error propagation in chain
    Console.WriteLine("⛓️ Demonstrating error propagation in chain:");
    var errorChainResult = await userService.GetUserByIdAsync(999) // This will fail
        .BindAsync(async user =>
        {
            Console.WriteLine("   This should not execute");
            return await userService.UpdateUserAsync(user.Id, new UpdateUserRequest());
        })
        .Map(user =>
        {
            Console.WriteLine("   This should also not execute");
            return user;
        });

    errorChainResult.Match(
        onSuccess: user => Console.WriteLine($"✅ This should not happen"),
        onFailure: error => Console.WriteLine($"❌ Error correctly propagated: {error.Message}")
    );
    Console.WriteLine();
}
