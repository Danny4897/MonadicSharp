using MonadicSharp;
using MonadicClean.Application.Common;
using MonadicClean.Application.DTOs;
using MonadicClean.Domain.Entities;
using MonadicClean.Domain.Repositories;

namespace MonadicClean.Application.Users.Commands;

public record CreateUserCommand(string Name, string Email) : ICommand<UserDto>;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        return await _userRepository.EmailExistsAsync(request.Email)
            .Bind(emailExists => emailExists
                ? Result<Unit>.Failure(Error.Create($"User with email {request.Email} already exists"))
                : Result<Unit>.Success(Unit.Value))
            .Bind(_ => User.Create(request.Name, request.Email))
            .BindAsync(user => _userRepository.AddAsync(user))
            .Map(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email.Value,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });
    }
}
