namespace OmniCore.Shared.Application.Exceptions;

using OmniCore.Shared.Domain.Abstractions;

/// <summary>
/// Exception thrown when an unexpected or unhandled failure occurs during application request processing.
/// </summary>
public class OmniCoreApplicationException : Exception
{
    public string RequestName { get; }
    public Error? Error { get; }

    public OmniCoreApplicationException(
        string requestName, 
        Error? error = null, 
        Exception? innerException = null)
        : base(FormatMessage(requestName, error), innerException)
    {
        RequestName = requestName;
        Error = error;
    }

    private static string FormatMessage(string requestName, Error? error)
    {
        if (error is null || error == Error.None)
        {
            return $"An unhandled error occurred while processing request '{requestName}'.";
        }

        return $"An application error '{error.Code}' occurred while processing request '{requestName}': {error.Description}";
    }
}