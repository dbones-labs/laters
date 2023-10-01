namespace Laters;

public class LatersException : Exception
{
    public LatersException() { }
    public LatersException(string message) : base(message) { }
    public LatersException(string message, Exception innerException): base(message, innerException) { }
}