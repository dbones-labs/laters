namespace Laters.Background;

using Data;
using Models;

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
            _scheduleCron.ManyForLater(name, jobPayload, cron, options, true);
        };
        _newSet.Add(name, upsert);
    }

    public void ForgetAboutAllOfIt<T>(string name, bool removeOrphins = true)
    {
        //we do nothing with this.
        _scheduleCron.ForgetAboutAllOfIt<T>(name, removeOrphins);
    }

    public async Task SaveChanges()
    {
        var existing = await _session.GetGlobalCronJobs();
        var toRemove = existing.Where(x=> !_newSet.ContainsKey(x.Id));

        foreach (var entry in _newSet)
        {
            entry.Value.Invoke();
        }

        foreach (var entry in toRemove)
        {
            _session.Delete<CronJob>(entry.Id);
            _session.DeleteOrphin(entry.Id);
        }
        
        await _session.SaveChanges();
    }
}