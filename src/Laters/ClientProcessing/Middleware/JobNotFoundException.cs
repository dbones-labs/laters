namespace Laters.ClientProcessing.Middleware;

using Exceptions;

/// <summary>
/// job not found, this is most likely due to the job being processed previously.
/// </summary>
public class JobNotFoundException : LatersException
{

    /// <summary>
    /// the job id that was not found
    /// </summary>
    /// <value></value>
    public string JobId { get; }

    /// <summary>
    /// create a new instance of <see cref="JobNotFoundException"/>
    /// </summary>
    /// <param name="jobId">the job id that was not found</param>
    public JobNotFoundException(string jobId) : base($"Cannot find {jobId}")
    {
        JobId = jobId;
    }
}