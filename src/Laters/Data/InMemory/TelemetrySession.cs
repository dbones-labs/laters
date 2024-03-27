namespace Laters.Data.InMemory;

using System.Collections.Generic;
using System.Threading.Tasks;
using Data;

/// <summary>
/// get telemetry data from the storage
/// </summary>
public class TelemetrySession : ITelemetrySession
{
    readonly InMemoryStore _store;


    /// <summary>
    /// create a new instance of <see cref="TelemetrySession"/>
    /// </summary>
    /// <param name="store">the datastore</param>
    public TelemetrySession(InMemoryStore store)
    {
        _store = store;
    }

    ///<inheritdoc/>
    public Task<List<JobCounter>> GetDeadletterJobs()
    {
        var counters = _store.Data.Values
            .Where(x => x is Job job && job.DeadLettered)
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
        var counters = _store.Data.Values
            .Where(x => x is Job job && job.ScheduledFor <= SystemDateTime.UtcNow && !job.DeadLettered)
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
        var counters = _store.Data.Values
            .Where(x => x is Job job && !job.DeadLettered)
            .GroupBy(x => x.JobType)
            .Select(x => new JobCounter()
            {
                JobType = x.Key,
                Count = x.Count()
            }).ToList();
        return Task.FromResult(counters);
    }
}