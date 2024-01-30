namespace Laters;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using Middleware;

using Pipes;

//CONFIGURE
public class OnceOptions
{
    public DateTime ScheduleFor { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Headers { get; set; } = new();
    public QualityDelivery Delivery { get; set; } = new();
}

public class QualityDelivery
{
    public int MaxRetries { get; set; } = 3;
    
    public int? TimeToLiveInSeconds { get; set; } = 300;
    
    /// <summary>
    /// will default to the global rate limit.
    /// </summary>
    public string? WindowName { get; set; }
}

public class CronOptions
{
    public Dictionary<string, string> Headers { get; set; } = new();
    public QualityDelivery Delivery { get; set; } = new();
    
    protected internal bool IsGlobalCron { get; set; } = false;
    public bool UpdateScheduledJobOnCronChange { get; set; } = true;
}



/// <summary>
/// schedule a job for later
/// </summary>
public interface ISchedule : IScheduleCron
{ 
    /// <summary>
    /// run once
    /// </summary>
    /// <param name="jobPayload">the data to run the job with</param>
    /// <param name="options">any extra parameters, if not supplied the task will be queued for immediate execution</param>
    /// <typeparam name="T">indicate the job being scheduled</typeparam>
    string ForLater<T>(T jobPayload, OnceOptions? options = null);

    /// <summary>
    /// run once
    /// </summary>
    /// <param name="jobPayload">the data to run the job with</param>
    /// <param name="scheduleFor">when to try and execute the job</param>
    /// <param name="options">any extra parameters, if not supplied the task will be queued for immediate execution</param>
    /// <typeparam name="T">indicate the job being scheduled</typeparam>
    string ForLater<T>(T jobPayload, DateTime scheduleFor, OnceOptions? options = null);
    
    /// <summary>
    /// remove a single scheduled job
    /// </summary>
    /// <param name="id">the id of the job to remove</param>
    void ForgetAboutIt<T>(string id);
    
}

/// <summary>
/// configure cron jobs
/// </summary>
public interface IScheduleCron
{
    /// <summary>
    /// set a job which will be executed on a Cron
    /// </summary>
    /// <param name="name">a unique name for the cron</param>
    /// <param name="jobPayload">the data for the job</param>
    /// <param name="cron">the cron on when to schedule the job instances</param>
    /// <param name="options">cron options</param>
    /// <typeparam name="T">the job to execute</typeparam>
    void ManyForLater<T>(
        [Required(AllowEmptyStrings = false)] string name, 
        T jobPayload,
        [Required(AllowEmptyStrings = false)] string cron, 
        CronOptions? options = null);

    
    /// <summary>
    /// remove a cron
    /// </summary>
    /// <param name="name">the name of the cron</param>
    /// <param name="removeOrphins">remove unprocessed jobs which are associated with this cron</param>
    /// <typeparam name="T">the job type</typeparam>
    void ForgetAboutAllOfIt<T>(
        [Required(AllowEmptyStrings = false)] string name, bool removeOrphins = true);
}

public interface IAdvancedSchedule : ISchedule
{
    string ForLaterNext(CronJob cronJob);
}

public class DefaultSchedule : IAdvancedSchedule
{
    private readonly ISession _session;
    readonly ICrontab _crontab;

    public DefaultSchedule(
        ISession session,
        ICrontab crontab)
    {
        _session = session;
        _crontab = crontab;
    }
    
    public virtual void ManyForLater<T>(string name, T jobPayload, string cron, CronOptions? options = null)
    {
        options ??= new CronOptions();
        var delivery = options.Delivery;

        var cronJob = new CronJob()
        {
            Id = name,
            Payload = JsonSerializer.Serialize(jobPayload),
            JobType = typeof(T).FullName,
            Headers = options.Headers,
            Cron = cron,
            MaxRetries = delivery.MaxRetries,
            TimeToLiveInSeconds = delivery.TimeToLiveInSeconds,
            WindowName = delivery.WindowName ?? LatersConstants.GlobalTumbler,
            IsGlobal = false
        };
        
        _session.Store(cronJob);
        ForLaterNext(cronJob);
    }

    public virtual void ForgetAboutAllOfIt<T>(string name, bool removeOrphins = true)
    {
        _session.Delete<Job>(name);
        if (removeOrphins)
        {
            _session.DeleteOrphin(name);
        }
    }

    public virtual string ForLater<T>(T jobPayload, OnceOptions? options = null)
    {
        options ??= new OnceOptions();
        var delivery = options.Delivery;
        
        var job = new Job()
        {
            Payload = JsonSerializer.Serialize(jobPayload),
            JobType = typeof(T).FullName,
            Headers = options.Headers,
            ScheduledFor = options.ScheduleFor,
            MaxRetries = delivery.MaxRetries,
            TimeToLiveInSeconds = delivery.TimeToLiveInSeconds,
            WindowName = delivery.WindowName ?? LatersConstants.GlobalTumbler
        };
        
        _session.Store(job);
        return job.Id;
    }

    public virtual string ForLater<T>(T jobPayload, DateTime scheduleFor, OnceOptions? options = null)
    {
        options ??= new OnceOptions();
        options.ScheduleFor = scheduleFor;
        
        return ForLater(jobPayload, options);
    }

    public virtual void ForgetAboutIt<T>(string id)
    {
        _session.Delete<Job>(id);
    }

    public string ForLaterNext(CronJob cronJob)
    {
        var job = cronJob.GetNextJob(_crontab);
        _session.Store(job);
        return job.Id;
    }
}

/// <summary>
/// setup a Global set of cron jobs.
/// items added via this will be treated as the complete set of global cron jobs
/// </summary>
public interface ISetupSchedule
{
    void Configure(IScheduleCron scheduleCron);
}

//Engine

//how do we want to get work done?

//scheduler can be on 1 server, and the workers can be the normal instances
//in this case the scheduler can monitor many databases (services)

//scheduler -> https -> workers (which will be loadbalanced)
//scheduler -> message -> workers (which will be loadbalanced)
//scheduler -> database -> workers (requires more work)


//tumberling window should be applied (we can send all the messages, which may not workout)
//placement of processing? 

public interface IDelegator
{
    
}

public interface IWorkerClient
{
    Task DelegateJob(ProcessJob processJob);
}

//Infra

public interface IScheduleCronAction : IAction<CronJob> { }

public interface IScheduleAction : IAction<Job> { }


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

public interface IProcessAction : IAction<Job> { }



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

public class DataContext<T> : JobContext<T>
{
    
}




