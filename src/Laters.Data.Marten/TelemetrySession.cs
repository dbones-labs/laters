namespace Laters.Data.Marten;

using global::Marten;
using Infrastructure;
using Models;

/// <summary>
/// get telemetry data from the storage
/// </summary>
public class TelemetrySession : ITelemetrySession
{
    readonly IQuerySession _querySession;

    /// <summary>
    /// create a new instance of <see cref="TelemetrySession"/>
    /// </summary>
    /// <param name="querySession">read sessions</param>
    public TelemetrySession(IQuerySession querySession)
    {
        _querySession = querySession;
    }

    ///<inheritdoc/>
    public Task<List<JobCounter>> GetDeadletterJobs()
    {
        var counters = _querySession.Query<Job>()
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
        var counters = _querySession.Query<Job>()
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
        var counters = _querySession.Query<Job>()
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