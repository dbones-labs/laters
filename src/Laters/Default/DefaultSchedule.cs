﻿namespace Laters.Default;

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

    /// <summary>
    /// creates a new instance of <see cref="DefaultSchedule"/>
    /// </summary>
    /// <param name="session">the storage session</param>
    /// <param name="crontab">the crontab</param>
    /// <param name="metrics">metics</param>
    /// <param name="traces">traces</param>
    public DefaultSchedule(
        ISession session,
        ICrontab crontab,
        IMetrics metrics,
        Traces traces)
    {
        _session = session;
        _crontab = crontab;
        _metrics = metrics;
        _traces = traces;
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
        
        _session.Store(cronJob);
        ForLaterNext(cronJob);
    }
    
    /// <inheritdoc />
    public virtual void ForgetAboutAllOfIt<T>(string name, bool removeOrphans = true)
    {
        _session.Delete<CronJob>(name);
        if (removeOrphans)
        {
            _session.DeleteOrphan(name);
        }
    }

    /// <inheritdoc />
    public virtual string ForLater<T>(T jobPayload, OnceOptions? options = null)
    {
        using var activity = _traces.StartActivity<T>(ActivityKind.Producer);

        options ??= new OnceOptions();
        var delivery = options.Delivery;        
        
        var job = new Job()
        {
            Payload = JsonSerializer.Serialize(jobPayload),
            JobType = typeof(T).FullName!,
            Headers = options.Headers,
            ScheduledFor = options.ScheduleFor,
            MaxRetries = delivery.MaxRetries,
            TimeToLiveInSeconds = delivery.TimeToLiveInSeconds,
            WindowName = delivery.WindowName ?? LatersConstants.GlobalTumbler,
            TraceId = activity?.Id
        };

        var tagList = new TagList
        {
            { Telemetry.JobType, job.JobType },
            { Telemetry.Window, job.WindowName }
        };
        
        _metrics.EnqueueCounter.Add(1, tagList);

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
        _session.Delete<Job>(id);
    }

    /// <inheritdoc />
    public virtual string ForLaterNext(CronJob cronJob)
    {
        var job = cronJob.GetNextJob(_crontab);
        _session.Store(job);
        return job.Id;
    }
}