namespace Laters.Default;

using System.Text.Json;
using ClientProcessing.Middleware;
using Data;
using Infrastucture.Cron;
using Models;
using ServerProcessing.Windows;

public class DefaultSchedule : IAdvancedSchedule
{
    readonly ISession _session;
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

    public virtual string ForLaterNext(CronJob cronJob)
    {
        var job = cronJob.GetNextJob(_crontab);
        _session.Store(job);
        return job.Id;
    }
}