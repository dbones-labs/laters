namespace Laters;

using Models;

public interface IAdvancedSchedule : ISchedule
{
    string ForLaterNext(CronJob cronJob);
}