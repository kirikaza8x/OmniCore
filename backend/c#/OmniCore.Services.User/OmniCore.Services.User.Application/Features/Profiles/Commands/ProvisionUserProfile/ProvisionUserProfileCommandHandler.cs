namespace OmniCore.Services.User.Application.Features.Profiles.Commands.ProvisionUserProfile;

using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class ProvisionUserProfileCommandHandler(IUserProfileRepository profiles)
    : ICommandHandler<ProvisionUserProfileCommand>
{
    public async Task<Result> Handle(ProvisionUserProfileCommand request, CancellationToken cancellationToken)
    {
        // The broker can deliver an event twice: an existing profile (even a deleted one) is a success, not an error.
        if (await profiles.ExistsAsync(UserId.From(request.AccountId), cancellationToken))
        {
            return Result.Success();
        }

        var created = UserProfile.Create(
            request.AccountId, request.Username, request.Email, request.FirstName, request.LastName);
        if (created.IsFailure)
        {
            return Result.Failure(created.Error);
        }

        profiles.Add(created.Value);
        return Result.Success();
    }
}
