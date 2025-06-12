using MonadicSharp;
using MonadicClean.Application.Common;
using MonadicClean.Domain.Repositories;

namespace MonadicClean.Application.Users.Commands;

public record DeleteUserCommand(int Id) : ICommand;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    private readonly IUserRepository _userRepository;

    public DeleteUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<Unit>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        return await _userRepository.ExistsAsync(request.Id)
            .Bind(exists => exists
                ? Result<Unit>.Success(Unit.Value)
                : Result<Unit>.Failure(Error.Create($"User with ID {request.Id} not found")))
            .BindAsync(_ => _userRepository.DeleteAsync(request.Id));
    }
}
