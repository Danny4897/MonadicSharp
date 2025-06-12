using MonadicSharp;
using MediatR;

namespace MonadicClean.Application.Common;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

public interface ICommand : IRequest<Result<Unit>>
{
}

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}

public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result<Unit>>
    where TCommand : ICommand
{
}
