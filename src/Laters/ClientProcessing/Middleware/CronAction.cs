namespace Laters.ClientProcessing.Middleware;

using Data;
using Infrastructure.Cron;
using Models;
using Dbones.Pipes;

/// <summary>
/// this will create a new job based on the parent cron job
/// </summary>
/// <typeparam name="T">the type of the job</typeparam>
public class CronAction<T> : IProcessAction<T>
{
    readonly ISession _session;
    readonly ICrontab _crontab;
    readonly ILogger<CronAction<T>> _logger;

    /// <summary>
    ///  this will create a new job based on the parent cron job
    /// </summary>
    /// <param name="session">database session</param>
    /// <param name="crontab">the crontab implementation</param>
    /// <param name="logger">logger</param>
    public CronAction(
        ISession session, 
        ICrontab crontab,
        ILogger<CronAction<T>> logger)
    {
        _session = session;
        _crontab = crontab;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        context.CancellationToken.ThrowIfCancellationRequested();       

        await next(context);
        
        var job = context.Job;

        //not part of a cron job
        if(job?.ParentCron is null) 
        { 
            return; 
        }

        var cronJob = await _session.GetById<CronJob>(job.ParentCron);
        if(cronJob is null) 
        {
            throw new MissingParentCronException(job.Id, job.ParentCron);
        }

        var nextJob = cronJob.GetNextJob(_crontab);
        
        _session.Store(nextJob);
        
        _logger.LogInformation("Completed job {completedJob}, parent cron job {parentCronJob}, new job {newJob}",
            context.JobId,
            cronJob.Id,
            nextJob.Id);
    }
}
