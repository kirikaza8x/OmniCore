namespace OmniCore.Shared.Application.Abstractions.Messaging;

using MediatR;
using OmniCore.Shared.Domain.Abstractions;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> 
    where TQuery : IQuery<TResponse>
{
}