namespace Laters.ClientProcessing.Middleware;

using Exceptions;

public class JobNotFoundException : LatersException
{
    public string JobId { get; }

    public JobNotFoundException(string jobId) : base($"Cannot find {jobId}")
    {
        JobId = jobId;
    }
}