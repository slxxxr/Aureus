namespace Aureus.UseCases.Common.Exceptions;

public abstract class UseCaseException(string message) : Exception(message)
{
    public abstract ErrorType ErrorType { get; }

    public abstract string ErrorCode { get; }
}
