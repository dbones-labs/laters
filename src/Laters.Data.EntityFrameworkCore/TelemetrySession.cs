namespace Laters.Data.EntityFrameworkCore;

using System.Data;
using Infrastructure;

/// <summary>
/// get telemetry data from the storage
/// </summary>
public class TelemetrySession : ITelemetrySession
{
    readonly LatersQueryDbContext _queryDbContext;

    /// <summary>
    /// create a new instance of <see cref="TelemetrySession"/>
    /// </summary>
    /// <param name="queryDbContext">read sessions</param>
    public TelemetrySession(LatersQueryDbContext queryDbContext)
    {
        _queryDbContext = queryDbContext;
    }

    ///<inheritdoc/>
    public Task<List<JobCounter>> GetDeadletterJobs()
    {
        var counters = _queryDbContext.Jobs
            .Where(x => x.DeadLettered)
            .GroupBy(x => x.JobType)
            .Select(x => new JobCounter()
            {
                JobType = x.Key,
                Count = x.Count()
            }).ToList();

        return Task.FromResult(counters);
    }

    ///<inheritdoc/>
    public Task<List<JobCounter>> GetReadyJobs()
    {
        var counters = _queryDbContext.Jobs
            .Where(x => x.ScheduledFor <= SystemDateTime.UtcNow && !x.DeadLettered)
            .GroupBy(x => x.JobType)
            .Select(x => new JobCounter()
            {
                JobType = x.Key,
                Count = x.Count()
            }).ToList();

        return Task.FromResult(counters);
    }

    ///<inheritdoc/>
    public Task<List<JobCounter>> GetScheduledJobs()
    {
        var counters = _queryDbContext.Jobs
            .Where(x => !x.DeadLettered)
            .GroupBy(x => x.JobType)
            .Select(x => new JobCounter()
            {
                JobType = x.Key,
                Count = x.Count()
            }).ToList();

        return Task.FromResult(counters);
    }
}