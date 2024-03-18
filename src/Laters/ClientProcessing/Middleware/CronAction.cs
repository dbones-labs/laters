namespace Laters.ClientProcessing.Middleware;

using Data;
using Infrastructure.Cron;
using Models;
using Dbones.Pipes;

public class CronAction<T> : IProcessAction<T>
{
    readonly ISession _session;
    readonly ICrontab _crontab;
    readonly ILogger<CronAction<T>> _logger;

    public CronAction(
        ISession session, 
        ICrontab crontab,
        ILogger<CronAction<T>> logger)
    {
        _session = session;
        _crontab = crontab;
        _logger = logger;
    }
    
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        await next(context);
        
        if(context.Job?.ParentCron is null) return;

        var cronJob = await _session.GetById<CronJob>(context.Job.ParentCron);
        var nextJob = cronJob.GetNextJob(_crontab);
        
        _session.Store(nextJob);
        
        _logger.LogInformation("Completed job {completedJob}, parent cron job {parentCronJob}, new job {newJob}",
            context.JobId,
            cronJob.Id,
            nextJob.Id);
    }
}