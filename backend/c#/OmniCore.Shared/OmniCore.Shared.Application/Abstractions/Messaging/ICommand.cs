namespace OmniCore.Shared.Application.Abstractions.Messaging;

using MediatR;
using OmniCore.Shared.Domain.Abstractions;

public interface ICommand : IRequest<Result>, IBaseCommand
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand
{
}

public interface IStreamCommand<out TResponse> : IStreamRequest<TResponse>, IBaseCommand
{
}