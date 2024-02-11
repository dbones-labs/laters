namespace Laters.AspNet;

using Exceptions;

public class NoJobTypeFoundException : LatersException
{
    public string JobType { get; }

    public NoJobTypeFoundException(string jobType) 
        : base($"Cannot find job {jobType}")
    {
        JobType = jobType;
    }
}