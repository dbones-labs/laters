namespace Laters.Infrastucture.Cron;

public interface ICrontab
{
    DateTime Next(string cronExpression);
}