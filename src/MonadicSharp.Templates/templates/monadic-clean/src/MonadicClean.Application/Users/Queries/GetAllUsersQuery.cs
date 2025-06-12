using MonadicSharp;
using MonadicClean.Application.Common;
using MonadicClean.Application.DTOs;
using MonadicClean.Domain.Repositories;

namespace MonadicClean.Application.Users.Queries;

public record GetAllUsersQuery : IQuery<IEnumerable<UserDto>>;

public class GetAllUsersQueryHandler : IQueryHandler<GetAllUsersQuery, IEnumerable<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetAllUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<IEnumerable<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        return await _userRepository.GetAllAsync()
            .Map(users => users.Select(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email.Value,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            }));
    }
}
