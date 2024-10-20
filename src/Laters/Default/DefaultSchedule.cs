namespace Laters.Default;

using System.Diagnostics;
using System.Text.Json;
using Data;
using Infrastructure;
using Infrastructure.Cron;
using Laters.Infrastructure.Telemetry;
using Models;

/// <summary>
/// the default schedule implementation
/// </summary>
public class DefaultSchedule : IAdvancedSchedule
{
    readonly ISession _session;
    readonly ICrontab _crontab;
    readonly IMetrics _metrics;
    readonly Traces _traces;
    readonly ILogger<DefaultSchedule> _logger;

    /// <summary>
    /// creates a new instance of <see cref="DefaultSchedule"/>
    /// </summary>
    /// <param name="session">the storage session</param>
    /// <param name="crontab">the crontab</param>
    /// <param name="metrics">metics</param>
    /// <param name="traces">traces</param>
    /// <param name="logger">the logger</param>
    public DefaultSchedule(
        ISession session,
        ICrontab crontab,
        IMetrics metrics,
        Traces traces,
        ILogger<DefaultSchedule> logger)
    {
        _session = session;
        _crontab = crontab;
        _metrics = metrics;
        _traces = traces;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public virtual void ManyForLater<T>(string name, T jobPayload, string cron, CronOptions? options = null)
    {
        options ??= new CronOptions();
        ManyForLater(name, jobPayload, cron, options, false);
    }

    /// <inheritdoc />
    public virtual void ManyForLater<T>(string name, T jobPayload, string cron, CronOptions? options, bool isGlobal)
    {
        options ??= new CronOptions();

        var delivery = options.Delivery; 
        var jobType = typeof(T).FullName!;
        var windowName = delivery.WindowName ?? LatersConstants.GlobalTumbler;

        //we use the scope for the log to allow for easy/consistent filtering
        using var _ = _logger.BeginScope(new Dictionary<string, string>
        {
            { Telemetry.Action, nameof(ManyForLater) },
            { Telemetry.JobType, jobType },
            { Telemetry.Window, windowName },
            { Telemetry.TraceId, Activity.Current?.Id ?? "" }
        });

        var cronJob = new CronJob()
        {
            Id = name,
            Payload = JsonSerializer.Serialize(jobPayload),
            JobType = typeof(T).FullName!,
            Headers = options.Headers,
            Cron = cron,
            MaxRetries = delivery.MaxRetries,
            TimeToLiveInSeconds = delivery.TimeToLiveInSeconds,
            WindowName = delivery.WindowName ?? LatersConstants.GlobalTumbler,
            IsGlobal = isGlobal
        };
        
        _logger.LogInformation("CronJob {JobId} scheduled for {Cron}", cronJob.Id, cronJob.Cron);
        _session.Store(cronJob);

        ForLaterNext(cronJob);
        cronJob.LastTimeJobSynced = DateTime.UtcNow;
    }
    
    /// <inheritdoc />
    public virtual void ForgetAboutAllOfIt<T>(string name, bool removeOrphans = true)
    {
        var jobType = typeof(T).FullName!;

        //we use the scope for the log to allow for easy/consistent filtering
        using var _ = _logger.BeginScope(new Dictionary<string, string>
        {
            { Telemetry.Action, nameof(ForgetAboutAllOfIt) },
            { Telemetry.JobType, jobType },
            { Telemetry.TraceId, Activity.Current?.Id ?? "" }
        });

        _logger.LogInformation("CronJob {Name} will be forgotten", name);
        _session.Delete<CronJob>(name);
        if (removeOrphans)
        {
            _session.DeleteOrphan(name);
        }
    }

    /// <inheritdoc />
    public virtual string ForLater<T>(T jobPayload, OnceOptions? options = null)
    {
        //setup the meta information
        options ??= new OnceOptions();

        var delivery = options.Delivery; 
        var jobType = typeof(T).FullName!;
        var windowName = delivery.WindowName ?? LatersConstants.GlobalTumbler;

        //setup telemetry, note we are using the scope for the log to allow for easy/consistent filtering
        using var activity = _traces.StartActivity<T>(ActivityKind.Producer);
        using var _ = _logger.BeginScope(new Dictionary<string, string>
        {
            { Telemetry.Action, nameof(ForLater) },
            { Telemetry.JobType, jobType },
            { Telemetry.Window, windowName },
            { Telemetry.TraceId, activity?.Id ?? "" }
        });
        
        var job = new Job()
        {
            Payload = JsonSerializer.Serialize(jobPayload),
            JobType = jobType,
            Headers = options.Headers,
            ScheduledFor = options.ScheduleFor,
            MaxRetries = delivery.MaxRetries,
            TimeToLiveInSeconds = delivery.TimeToLiveInSeconds,
            WindowName = windowName,
            TraceId = activity?.Id
        };

        var tagList = new TagList
        {
            { Telemetry.JobType, job.JobType },
            { Telemetry.Window, job.WindowName }
        };
        
        _metrics.EnqueueCounter.Add(1, tagList);

        _logger.LogInformation("Job {JobId} scheduled for {ScheduledFor}", job.Id, job.ScheduledFor);
        _session.Store(job);
        return job.Id;
    }

    /// <inheritdoc />
    public virtual string ForLater<T>(T jobPayload, DateTime scheduleFor, OnceOptions? options = null)
    {
        options ??= new OnceOptions();
        options.ScheduleFor = scheduleFor;
        
        return ForLater(jobPayload, options);
    }

    /// <inheritdoc />
    public virtual void ForgetAboutIt<T>(string id)
    {
        var jobType = typeof(T).FullName!;

        //we use the scope for the log to allow for easy/consistent filtering
        using var _ = _logger.BeginScope(new Dictionary<string, string>
        {
            { Telemetry.Action, nameof(ForgetAboutIt) },
            { Telemetry.JobType, jobType },
            { Telemetry.TraceId, Activity.Current?.Id ?? "" }
        });

        _logger.LogInformation("Job {JobId} will be forgotten", id);
        _session.Delete<Job>(id);
    }

    /// <inheritdoc />
    public virtual void ForLaterNext(CronJob cronJob)
    {
        var job = cronJob.GetNextJob(_crontab);
        _session.Store(job);
    }
}