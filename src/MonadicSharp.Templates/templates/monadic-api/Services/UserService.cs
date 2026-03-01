using MonadicSharp;
using MonadicSharp.Extensions;
using MonadicApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace MonadicApi.Services;

public interface IUserService
{
    Task<Result<IEnumerable<UserResponse>>> GetAllUsersAsync();
    Task<Result<UserResponse>> GetUserByIdAsync(int id);
    Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request);
    Task<Result<UserResponse>> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<Result<Unit>> DeleteUserAsync(int id);
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<UserResponse>>> GetAllUsersAsync()
    {
        try
        {
            var users = await _context.Users
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Result<IEnumerable<UserResponse>>.Success(users);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<UserResponse>>.Failure(Error.Create($"Failed to retrieve users: {ex.Message}"));
        }
    }

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
            .Map(user => new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
    }
    public async Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request)
    {
        return await ValidateCreateUserRequest(request).BindAsync(async validRequest =>
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == validRequest.Email);

                if (existingUser.HasValue)
                {
                    return Result<UserResponse>.Failure(Error.Create($"User with email {validRequest.Email} already exists"));
                }

                var user = new User
                {
                    Name = validRequest.Name,
                    Email = validRequest.Email,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Result<UserResponse>.Success(new UserResponse
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                });
            });
    }

    public async Task<Result<UserResponse>> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        return await ValidateUserId(id)
            .Bind(_ => ValidateUpdateUserRequest(request))
            .BindAsync(async _ =>
            {
                var user = await _context.Users.FindAsync(id);
                return user != null
                    ? Result<User>.Success(user)
                    : Result<User>.Failure(Error.Create($"User with ID {id} not found"));
            })
            .BindAsync(async user =>
            {
                // Check email uniqueness if email is being updated
                if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != id);

                    if (existingUser.HasValue)
                    {
                        return Result<User>.Failure(Error.Create($"Email {request.Email} is already in use"));
                    }
                }

                // Update user properties
                if (!string.IsNullOrEmpty(request.Name))
                    user.Name = request.Name;

                if (!string.IsNullOrEmpty(request.Email))
                    user.Email = request.Email;

                if (request.IsActive.HasValue)
                    user.IsActive = request.IsActive.Value;

                await _context.SaveChangesAsync();

                return Result<User>.Success(user);
            })
            .Map(user => new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
    }

    public async Task<Result<Unit>> DeleteUserAsync(int id)
    {
        return await ValidateUserId(id)
            .BindAsync(async validId =>
            {
                var user = await _context.Users.FindAsync(validId);
                if (user == null)
                {
                    return Result<Unit>.Failure(Error.Create($"User with ID {validId} not found"));
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Result<Unit>.Success(Unit.Value);
            });
    }

    private static Result<int> ValidateUserId(int id)
    {
        return id > 0
            ? Result<int>.Success(id)
            : Result<int>.Failure(Error.Create("User ID must be greater than 0"));
    }

    private static Result<CreateUserRequest> ValidateCreateUserRequest(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<CreateUserRequest>.Failure(Error.Create("Name is required"));

        if (request.Name.Length > 100)
            return Result<CreateUserRequest>.Failure(Error.Create("Name cannot exceed 100 characters"));

        if (string.IsNullOrWhiteSpace(request.Email))
            return Result<CreateUserRequest>.Failure(Error.Create("Email is required"));

        if (!IsValidEmail(request.Email))
            return Result<CreateUserRequest>.Failure(Error.Create("Invalid email format"));

        return Result<CreateUserRequest>.Success(request);
    }

    private static Result<UpdateUserRequest> ValidateUpdateUserRequest(UpdateUserRequest request)
    {
        if (!string.IsNullOrEmpty(request.Name) && request.Name.Length > 100)
            return Result<UpdateUserRequest>.Failure(Error.Create("Name cannot exceed 100 characters"));

        if (!string.IsNullOrEmpty(request.Email) && !IsValidEmail(request.Email))
            return Result<UpdateUserRequest>.Failure(Error.Create("Invalid email format"));

        return Result<UpdateUserRequest>.Success(request);
    }

    private static bool IsValidEmail(string email)
    {
        var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailPattern);
    }
}
