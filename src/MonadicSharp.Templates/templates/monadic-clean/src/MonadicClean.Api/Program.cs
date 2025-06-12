using MonadicSharp;
using MonadicClean.Application.Common;
using MonadicClean.Application.Users.Queries;
using MonadicClean.Application.Users.Commands;
using MonadicClean.Application.DTOs;
using MonadicClean.Infrastructure.Data;
using MonadicClean.Infrastructure.Repositories;
using MonadicClean.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Entity Framework with In-Memory Database for demo
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("MonadicCleanDb"));

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetAllUsersQuery).Assembly));

// Add repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData(context);
}

app.Run();

static async Task SeedData(AppDbContext context)
{
    if (!context.Users.Any())
    {
        var users = new[]
        {
            new MonadicClean.Domain.Entities.User("John Doe", "john@example.com"),
            new MonadicClean.Domain.Entities.User("Jane Smith", "jane@example.com"),
            new MonadicClean.Domain.Entities.User("Bob Johnson", "bob@example.com")
        };

        // Note: In real implementation, use proper factory methods with Result<T>
        context.Users.AddRange(users);
        await context.SaveChangesAsync();
    }
}
