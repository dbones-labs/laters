namespace Laters.ClientProcessing.Middleware;

public interface ICrontab
{
    DateTime Next(string cronExpression);
}