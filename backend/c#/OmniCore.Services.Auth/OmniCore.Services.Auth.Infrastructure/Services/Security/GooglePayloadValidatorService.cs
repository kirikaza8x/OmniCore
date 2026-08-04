namespace OmniCore.Services.Auth.Infrastructure.Services.Security;

using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Infrastructure.Configs;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GooglePayloadValidatorService(
    IOptions<GoogleAuthConfig> configs) : IGooglePayloadValidatorService
{
    private readonly GoogleAuthConfig _configs = configs.Value;

    public async Task<Result<GoogleJsonWebSignature.Payload>> ValidateAsync(
        string idToken, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configs.ServerClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return Result.Success(payload);
        }
        catch (InvalidJwtException ex)
        {
            return Result.Failure<GoogleJsonWebSignature.Payload>(
                Error.Validation("GoogleAuth.InvalidToken", $"Google token validation failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Result.Failure<GoogleJsonWebSignature.Payload>(
                Error.Failure("GoogleAuth.ValidationError", $"Unexpected error during Google token validation: {ex.Message}"));
        }
    }
}