namespace OmniCore.Shared.Domain.Abstractions;

public interface IValidationResult
{
    public static readonly Error ValidationError = Error.Validation("ValidationError", "Validation failed");
    
    IReadOnlyCollection<Error> Errors { get; }
}