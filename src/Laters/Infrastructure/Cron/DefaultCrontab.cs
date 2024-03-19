namespace Laters.Infrastructure.Cron;

using NCrontab;

public class DefaultCrontab : ICrontab
{
    public DateTime Next(string cronExpression)
    {
        var schedule = CrontabSchedule.Parse(cronExpression);
        var result = schedule.GetNextOccurrence(SystemDateTime.UtcNow);
        return result;
    }
}