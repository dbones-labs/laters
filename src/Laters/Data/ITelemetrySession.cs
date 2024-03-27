namespace Laters.Data;

/// <summary>
/// get telemetry data from the storage
/// </summary>
public interface ITelemetrySession
{
    /// <summary>
    /// get the number of jobs which are waiting to be processed
    /// </summary>
    /// <returns>Counters</returns>
    Task<List<JobCounter>> GetReadyJobs();

    /// <summary>
    /// get the total number of jobs which are scheduled
    /// </summary>
    /// <returns>Counters</returns>
    Task<List<JobCounter>> GetScheduledJobs();

    /// <summary>
    /// the the number of jobs which have been deadlettered
    /// </summary>
    /// <returns>Counters</returns>
    Task<List<JobCounter>> GetDeadletterJobs();
}
