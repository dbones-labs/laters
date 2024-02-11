namespace Laters.AspNet;

using Exceptions;

/// <summary>
/// each job which is fired for processing must have a handler
///
/// confirm that you have registered your handler correctly
/// </summary>
public class NoJobTypeFoundException : LatersException
{
    public string JobType { get; }

    public NoJobTypeFoundException(string jobType) 
        : base($"Cannot find job {jobType}")
    {
        JobType = jobType;
    }
}