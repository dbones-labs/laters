namespace Laters.AspNet;

using Exceptions;

/// <summary>
/// we only support one handler to one job type.
/// </summary>
public class JobTypeWithMoreThanOneHandler : LatersException
{
    public string JobType { get; }

    public JobTypeWithMoreThanOneHandler(string jobType)
        : base($"there is more than one handler for {jobType}")
    {
        JobType = jobType;
    }
}