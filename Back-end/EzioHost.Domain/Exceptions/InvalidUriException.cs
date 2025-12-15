namespace EzioHost.Domain.Exceptions;

[Serializable]
public class InvalidUriException : Exception
{
    public InvalidUriException(string message)
        : base(message)
    {
    }

    public InvalidUriException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}