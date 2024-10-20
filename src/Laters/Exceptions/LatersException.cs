namespace Laters.Exceptions;

/// <summary>
/// base exception for Laters
/// </summary>
public abstract class LatersException : Exception
{
    protected LatersException(string message) : base(message) { }
    protected LatersException(string message, Exception innerException): base(message, innerException) { }
}