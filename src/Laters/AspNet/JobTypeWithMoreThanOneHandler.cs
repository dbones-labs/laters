namespace Laters.AspNet;

using Exceptions;

public class JobTypeWithMoreThanOneHandler : LatersException
{
    public string JobType { get; }

    public JobTypeWithMoreThanOneHandler(string jobType)
        : base($"there is more than one handler for {jobType}")
    {
        JobType = jobType;
    }
}