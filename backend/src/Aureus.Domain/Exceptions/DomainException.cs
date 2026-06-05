namespace Aureus.Domain.Exceptions;

public abstract class DomainException(string message) : Exception(message)
{
    public abstract ErrorType ErrorType { get; }

    public abstract string ErrorCode { get; }
}
