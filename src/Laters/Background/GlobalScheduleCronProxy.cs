namespace Laters.Background;

using System.Text.Json;
using Data;
using Laters.Infrastructure;
using Models;

/// <summary>
/// this is a proxy for the global schedule cron, it will allow us to
/// upsert and remove global cron jobs.
/// </summary>
public class GlobalScheduleCronProxy : IScheduleCron
{
    readonly ISession _session;
    readonly IAdvancedSchedule _scheduleCron;
    readonly Dictionary<string, Action> _newSet = new();

    public GlobalScheduleCronProxy(ISession session, IAdvancedSchedule scheduleCron)
    {
        _session = session;
        _scheduleCron = scheduleCron;
    }
    
    public void ManyForLater<T>(string name, T jobPayload, string cron, CronOptions? options = null)
    {
        Action upsert = () =>
        {
            var cronJob = _session.GetById<CronJob>(name).Result;
            if (cronJob is null)
            {
                _scheduleCron.ManyForLater(name, jobPayload, cron, options);
                return;
            }

            //some storage engines may not support just upserting, thus we loaded
            //the cronjob and we will update it, causing it be marked as dirty.
            options ??= new CronOptions();
            var delivery = options.Delivery;

            cronJob.Payload = JsonSerializer.Serialize(jobPayload);
            cronJob.JobType = typeof(T).FullName!;
            cronJob.Headers = options.Headers;  
            cronJob.Cron = cron;
            cronJob.MaxRetries = delivery.MaxRetries;
            cronJob.TimeToLiveInSeconds = delivery.TimeToLiveInSeconds;
            cronJob.WindowName = delivery.WindowName ?? LatersConstants.GlobalTumbler;
            cronJob.IsGlobal = true;
        };
        _newSet.Add(name, upsert);
    }

    ///<inheritdoc/>
    public void ForgetAboutAllOfIt<T>(string name, bool removeOrphins = true)
    {
        //we do nothing with this.
        _scheduleCron.ForgetAboutAllOfIt<T>(name, removeOrphins);
    }

    ///<inheritdoc/>    
    public async Task SaveChanges(CancellationToken cancellationToken = default)
    {
        var existing = await _session.GetGlobalCronJobs();
        var toRemove = existing.Where(x=> !_newSet.ContainsKey(x.Id)).ToList();

        foreach (var entry in _newSet)
        {
            entry.Value.Invoke();
        }

        foreach (var entry in toRemove)
        {
            _session.Delete<CronJob>(entry.Id);
            _session.DeleteOrphan(entry.Id);
        }
        
        await _session.SaveChanges(cancellationToken);
    }
}
