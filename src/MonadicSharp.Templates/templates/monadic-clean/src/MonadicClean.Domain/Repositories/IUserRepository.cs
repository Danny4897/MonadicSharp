using MonadicSharp;
using MonadicClean.Domain.Entities;

namespace MonadicClean.Domain.Repositories;

public interface IUserRepository
{
    Task<Result<IEnumerable<User>>> GetAllAsync();
    Task<Result<Option<User>>> GetByIdAsync(int id);
    Task<Result<Option<User>>> GetByEmailAsync(string email);
    Task<Result<User>> AddAsync(User user);
    Task<Result<User>> UpdateAsync(User user);
    Task<Result<Unit>> DeleteAsync(int id);
    Task<Result<bool>> ExistsAsync(int id);
    Task<Result<bool>> EmailExistsAsync(string email, int? excludeUserId = null);
}
