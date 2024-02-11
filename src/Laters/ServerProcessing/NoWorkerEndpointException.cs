namespace Laters.ServerProcessing;

using Configuration;
using Exceptions;

/// <summary>
/// ensure you have set a endpoint in the <see cref="Setup"/>
/// </summary>
public class NoWorkerEndpointException : LatersException
{
    public NoWorkerEndpointException() : base("please provide a worker-endpoint")
    {
    }

    protected NoWorkerEndpointException(string message, Exception innerException) : base(message, innerException)
    {
    }
}