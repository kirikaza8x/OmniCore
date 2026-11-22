namespace OmniCore.Services.User.Application.Features.Profiles.Commands.ProvisionUserProfile;

using OmniCore.Shared.Application.Abstractions.Messaging;

/// <summary>
/// Creates the profile for a newly registered account. Sent only by the broker consumer (ADR 0004), never by an endpoint,
/// so the id is the account id from Auth's event, not something a client chooses.
/// </summary>
public record ProvisionUserProfileCommand(
    Guid AccountId,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName) : ICommand;
