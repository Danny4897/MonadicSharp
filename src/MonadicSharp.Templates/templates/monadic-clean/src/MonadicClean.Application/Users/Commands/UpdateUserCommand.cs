using MonadicSharp;
using MonadicClean.Application.Common;
using MonadicClean.Application.DTOs;
using MonadicClean.Domain.Repositories;

namespace MonadicClean.Application.Users.Commands;

public record UpdateUserCommand(int Id, string? Name, string? Email, bool? IsActive) : ICommand<UserDto>;

public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        return await _userRepository.GetByIdAsync(request.Id)
            .Bind(userOption => userOption.Match(
                some: user => Result<MonadicClean.Domain.Entities.User>.Success(user),
                none: () => Result<MonadicClean.Domain.Entities.User>.Failure(Error.Create($"User with ID {request.Id} not found"))
            ))
            .BindAsync(async user =>
            {
                // Check if email is being updated and if it already exists
                if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email.Value)
                {
                    var emailExists = await _userRepository.EmailExistsAsync(request.Email, request.Id);
                    return await emailExists.Bind(exists => exists
                        ? Result<MonadicClean.Domain.Entities.User>.Failure(Error.Create($"Email {request.Email} is already in use"))
                        : Result<MonadicClean.Domain.Entities.User>.Success(user));
                }
                return Result<MonadicClean.Domain.Entities.User>.Success(user);
            })
            .Bind(user =>
            {
                // Apply updates
                var result = Result<MonadicClean.Domain.Entities.User>.Success(user);

                if (!string.IsNullOrEmpty(request.Name))
                {
                    result = result.Bind(u => u.UpdateName(request.Name).Map(_ => u));
                }

                if (!string.IsNullOrEmpty(request.Email))
                {
                    result = result.Bind(u => u.UpdateEmail(request.Email).Map(_ => u));
                }

                if (request.IsActive.HasValue)
                {
                    if (request.IsActive.Value)
                        user.Activate();
                    else
                        user.Deactivate();
                }

                return result;
            })
            .BindAsync(user => _userRepository.UpdateAsync(user))
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
