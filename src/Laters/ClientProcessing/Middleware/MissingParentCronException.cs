namespace Laters.ClientProcessing.Middleware;
using Laters.Exceptions;

/// <summary>
/// this will create a new job based on the parent cron job
/// </summary>
public class MissingParentCronException : LatersException
{
    /// <summary>
    /// create a new instance of the exception
    /// </summary>
    /// <param name="jobId">the current job</param>
    /// <param name="parentCronJob">the parent Cron job of the current job</param>
    public MissingParentCronException(string jobId, string parentCronJob) : base($"CronJob not found, CronId: {jobId}, ParentCron: {parentCronJob}")
    {
        JobId = jobId;
        ParentCronJob = parentCronJob;
    }

    /// <summary>
    /// the job id, which we cannot find the parent cron job for
    /// </summary> 
    public string JobId { get; }

    /// <summary>
    /// the parent cron job we are looking for
    /// </summary>
    public string ParentCronJob { get; }
}
