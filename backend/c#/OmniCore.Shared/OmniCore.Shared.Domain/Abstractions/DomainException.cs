namespace OmniCore.Shared.Domain.Abstractions;

public abstract class DomainException : Exception
{
    public Error Error { get; }

    protected DomainException(Error error) : base(error.Description)
    {
        Error = error;
    }
}