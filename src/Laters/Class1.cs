//Infra
/*
//i feel the compiler ignores my comments.


namespace Laters;

using System.Diagnostics;
using Pipes;


public class InvokeJobHandlerAction : IProcessAction
{
    readonly IServiceProvider _scope;
    readonly JobDelegates _jobDelegates;

    public InvokeJobHandlerAction(IServiceProvider scope, JobDelegates jobDelegates)
    {
        _scope = scope;
        _jobDelegates = jobDelegates;
    }
    
    public async Task Execute(Job context, Next<Job> next)
    {
        await _jobDelegates.Execute(_scope, context);
    }
}

public interface IScheduleCronAction : IAction<CronJob> { }

public interface IScheduleAction : IAction<Job> { }



public interface IProcessAction : IAction<Job> { }


public class TelemetryScheduleAction : IScheduleAction
{
    private readonly Telemetry _telemetry;

    public TelemetryScheduleAction(Telemetry telemetry)
    {
        _telemetry = telemetry;
    }

    public async Task Execute(Job context, Next<Job> next)
    {
        var name = $"laters schedule job: {context.JobType}";
        var traceId = Activity.Current?.Id;

        var activity = traceId != null
            ? _telemetry.ActivitySource.StartActivity(name, ActivityKind.Internal, traceId)
            : _telemetry.ActivitySource.StartActivity(name, ActivityKind.Internal);

        Activity.Current ??= activity;

        using (activity)
        {
            await next(context);
        }
    }
}

public class OpenTelemetryProcessAction : IProcessAction
{
    readonly Telemetry _telemetry;
    readonly TelemetryContext _telemetryContext;

    public OpenTelemetryProcessAction(
        Telemetry telemetry,
        TelemetryContext telemetryContext)
    {
        _telemetry = telemetry;
        _telemetryContext = telemetryContext;
    }


    public async Task Execute(Job context, Next<Job> next)
    {
        if (context.Headers.TryGetValue(Telemetry.Header, out var traceId))
        {

        }


        var name = $"laters job handler: {context.JobType}";
        var activity = traceId != null
            ? _telemetry.ActivitySource.StartActivity(name, ActivityKind.Internal, traceId)
            : _telemetry.ActivitySource.StartActivity(name, ActivityKind.Internal);

        Activity.Current = activity;
        _telemetryContext.OpenTelemetryTraceId = activity?.Id;

        using (activity)
        {
            await next(context);
        }
    }
}


*/

