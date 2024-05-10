namespace Laters.ClientProcessing.Middleware;

using System.Diagnostics;
using Dbones.Pipes;
using Laters.Infrastructure.Telemetry;
using Laters.ServerProcessing;

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
        var candidate = context.ServerRequested;
        using var activity = _telemetry.StartActivity(candidate.JobType, ActivityKind.Consumer);
        if (activity != null)
        {
            Activity.Current = activity;
            activity.AddTag(Telemetry.LeaderId, _leaderContext.ServerId);
            activity.AddTag(Telemetry.JobId, candidate.Id);
            activity.AddTag(Telemetry.JobType, candidate.JobType);
            activity.AddTag(Telemetry.Window, candidate.Window);
        }
        
        await next(context);
        
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}