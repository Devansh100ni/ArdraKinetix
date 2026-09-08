namespace TaskBoard.Domain.Exceptions;

public class DomainValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public DomainValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]> { { string.Empty, [message] } };
    }

    public DomainValidationException(string propertyName, string errorMessage) : base(errorMessage)
    {
        Errors = new Dictionary<string, string[]> { { propertyName, [errorMessage] } };
    }

    public DomainValidationException(IReadOnlyDictionary<string, string[]> errors) : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }
}
