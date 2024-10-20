namespace Laters;

using Models;

public interface IAdvancedSchedule : ISchedule
{
    void ManyForLater<T>(string name, T jobPayload, string cron, CronOptions options, bool isGlobal);
    
    void ForLaterNext(CronJob cronJob);
}