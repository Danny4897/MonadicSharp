using Microsoft.EntityFrameworkCore;
using MonadicSharp;
using MonadicClean.Domain.Entities;
using MonadicClean.Domain.Repositories;
using MonadicClean.Infrastructure.Data;

namespace MonadicClean.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IEnumerable<User>>> GetAllAsync()
    {
        try
        {
            var users = await _context.Users.ToListAsync();
            return Result<IEnumerable<User>>.Success(users);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<User>>.Failure(Error.Create($"Failed to retrieve users: {ex.Message}"));
        }
    }

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

    public async Task<Result<Option<User>>> GetByEmailAsync(string email)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Value == email);
            return Result<Option<User>>.Success(Option<User>.From(user));
        }
        catch (Exception ex)
        {
            return Result<Option<User>>.Failure(Error.Create($"Failed to retrieve user by email: {ex.Message}"));
        }
    }

    public async Task<Result<User>> AddAsync(User user)
    {
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Result<User>.Success(user);
        }
        catch (Exception ex)
        {
            return Result<User>.Failure(Error.Create($"Failed to add user: {ex.Message}"));
        }
    }

    public async Task<Result<User>> UpdateAsync(User user)
    {
        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return Result<User>.Success(user);
        }
        catch (Exception ex)
        {
            return Result<User>.Failure(Error.Create($"Failed to update user: {ex.Message}"));
        }
    }

    public async Task<Result<Unit>> DeleteAsync(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Result<Unit>.Failure(Error.Create($"User with ID {id} not found"));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure(Error.Create($"Failed to delete user {id}: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> ExistsAsync(int id)
    {
        try
        {
            var exists = await _context.Users.AnyAsync(u => u.Id == id);
            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(Error.Create($"Failed to check if user exists: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> EmailExistsAsync(string email, int? excludeUserId = null)
    {
        try
        {
            var query = _context.Users.Where(u => u.Email.Value == email);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            var exists = await query.AnyAsync();
            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(Error.Create($"Failed to check if email exists: {ex.Message}"));
        }
    }
}
