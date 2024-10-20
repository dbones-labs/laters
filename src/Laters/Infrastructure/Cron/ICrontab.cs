namespace Laters.Infrastructure.Cron;

public interface ICrontab
{
    DateTime Next(string cronExpression);
}