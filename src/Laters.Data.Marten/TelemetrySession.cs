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
        //need to replace with a native sql query
        var jobTypes = _querySession.Query<Job>().Select(x => x.JobType).Distinct();

        List<JobCounter> counters = new();

        foreach (var jobType in jobTypes)
        {
            var count = _querySession.Query<Job>()
                .Where(x => x.JobType == jobType)
                .Where(x => x.DeadLettered)
                .Count();

            counters.Add(new() { Count = count, JobType = jobType });
        }

        return Task.FromResult(counters);
    }

    ///<inheritdoc/>
    public Task<List<JobCounter>> GetReadyJobs()
    {
        //need to replace with a native sql query
        var jobTypes = _querySession.Query<Job>().Select(x => x.JobType).Distinct();

        List<JobCounter> counters = new();

        foreach (var jobType in jobTypes)
        {
            var count = _querySession.Query<Job>()
                .Where(x => x.JobType == jobType)
                .Where(x => x.ScheduledFor <= SystemDateTime.UtcNow)
                .Where(x => !x.DeadLettered)
                .Count();

            counters.Add(new() { Count = count, JobType = jobType });
        }

        return Task.FromResult(counters);
    }

    ///<inheritdoc/>
    public Task<List<JobCounter>> GetScheduledJobs()
    {
        //need to replace with a native sql query
        var jobTypes = _querySession.Query<Job>().Select(x => x.JobType).Distinct();

        List<JobCounter> counters = new();

        foreach (var jobType in jobTypes)
        {
            var count = _querySession.Query<Job>()
                .Where(x => x.JobType == jobType)
                .Where(x => !x.DeadLettered)
                .Count();

            counters.Add(new() { Count = count, JobType = jobType });
        }

        return Task.FromResult(counters);
    }
}