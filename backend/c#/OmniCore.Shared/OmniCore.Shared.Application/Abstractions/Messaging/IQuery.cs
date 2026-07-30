namespace OmniCore.Shared.Application.Abstractions.Messaging;

using MediatR;
using OmniCore.Shared.Domain.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}