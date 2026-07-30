namespace OmniCore.Shared.Application.Abstractions.Messaging;

using MediatR;
using OmniCore.Shared.Domain.Abstractions;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result> 
    where TCommand : ICommand
{
}

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>> 
    where TCommand : ICommand<TResponse>
{
}

public interface IStreamCommandHandler<in TCommand, TResponse> : IStreamRequestHandler<TCommand, TResponse>
    where TCommand : IStreamCommand<TResponse>
{
}