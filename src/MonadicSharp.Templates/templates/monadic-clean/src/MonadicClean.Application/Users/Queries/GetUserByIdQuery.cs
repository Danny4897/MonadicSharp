using MonadicSharp;
using MonadicClean.Application.Common;
using MonadicClean.Application.DTOs;
using MonadicClean.Domain.Repositories;

namespace MonadicClean.Application.Users.Queries;

public record GetUserByIdQuery(int Id) : IQuery<UserDto>;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await _userRepository.GetByIdAsync(request.Id)
            .Bind(userOption => userOption.Match(
                some: user => Result<UserDto>.Success(new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email.Value,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                }),
                none: () => Result<UserDto>.Failure(Error.Create($"User with ID {request.Id} not found"))
            ));
    }
}
