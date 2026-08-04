namespace OmniCore.Services.Auth.Application.Abstractions.Security;

using Google.Apis.Auth;
using OmniCore.Shared.Domain.Abstractions;

public interface IGooglePayloadValidatorService
{
    Task<Result<GoogleJsonWebSignature.Payload>> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}