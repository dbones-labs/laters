namespace Laters.ClientProcessing.Middleware;

using System.Diagnostics;
using Dbones.Pipes;
using Laters.Infrastructure.Telemetry;
using Laters.ServerProcessing;

/// <summary>
/// apply metrics to the job processing
/// </summary>
/// <typeparam name="T">the job type</typeparam>
public class MetricsAction<T> : IProcessAction<T>
{
    readonly IMetrics _metrics;

    /// <summary>
    /// creates a new instance of <see cref="MetricsAction{T}"/>
    /// </summary>
    /// <param name="metrics">metrics wrapper</param>
    public MetricsAction(IMetrics metrics)
    {
        _metrics = metrics;
    }

    /// <inheritdoc />
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        var candidate = context.Job!;
        var tagList = new TagList
        {
            { Telemetry.JobType, candidate.JobType },
            { Telemetry.Window, candidate.WindowName }
        };

        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
            _metrics.ProcessCounter.Add(1, tagList);
            _metrics.ProcessTime.Record(sw.Elapsed.TotalMilliseconds, tagList);
        }
        catch (Exception)
        {
            _metrics.ProcessErrorsCounter.Add(1, tagList);
            throw;
        }
    }
}


/// <summary>
/// trace the job processing
/// </summary>
/// <typeparam name="T">the job type</typeparam>
public class TraceAction<T> : IProcessAction<T>
{
    readonly Traces _telemetry;
    readonly LeaderContext _leaderContext;


    /// <summary>
    /// creates a new instance of <see cref="TraceAction{T}"/> 
    /// </summary>
    /// <param name="telemetry">the trace wrapper</param>
    /// <param name="leaderContext">the leader</param>
    public TraceAction(Traces telemetry, LeaderContext leaderContext)
    {
        _telemetry = telemetry;
        _leaderContext = leaderContext;
    }

    /// <inheritdoc />
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        var candidate = context.Job!;
        using var activity = _telemetry.StartActivity(candidate.JobType, ActivityKind.Consumer);
        if (activity != null)
        {
            Activity.Current = activity;
            activity.AddTag("leader.id", _leaderContext.ServerId);
            activity.AddTag("job.id", candidate.Id);
            activity.AddTag("job.type", candidate.JobType);
            activity.AddTag("job.windowName", candidate.WindowName);
        }
        

        await next(context);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}