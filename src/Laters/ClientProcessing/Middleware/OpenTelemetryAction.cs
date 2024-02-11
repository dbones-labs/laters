namespace Laters.ClientProcessing.Middleware;

using System.Text.Json;
using Data;
using Exceptions;
using Mnimal;
using Models;
using NCrontab;
using Pipes;

/// <summary>
/// these actions are the Job Handling pipeline (remember to register with the IoC container)
/// </summary>
public class ClientActions
{
    /// <summary>
    /// handle the dead-letter and backoff (applied first)
    /// </summary>
    public Type FailureAction { get; set; } = typeof(FailureAction<>);
    
    /// <summary>
    /// pulls the data into memory for the rest of the pipeline to make use of (applied second)
    /// </summary>
    public Type LoadJobIntoContextAction { get; set; } = typeof(LoadJobIntoContextAction<>);
    
    /// <summary>
    /// onced processed if the job is part of a cronjob, then create and queue next (applied third)
    /// </summary>
    public Type QueueNextAction { get; set; } = typeof(QueueNextAction<>);
    
    /// <summary>
    /// any and all custom actions (applied 4th and onwards, in order)
    /// </summary>
    public List<Type> CustomActions { get; set; } = new();

    /// <summary>
    /// this is the handler action, which will execute the job (applied last)
    /// </summary>
    public Type MainAction { get; set; } = typeof(HandlerAction<>);
}


public class FailureAction<T> : IProcessAction<T>
{
    static Random _random = new();
    
    readonly IServiceProvider _serviceProvider;
    readonly ILogger<FailureAction<T>> _logger;

    public FailureAction(
        IServiceProvider serviceProvider,
        ILogger<FailureAction<T>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        try
        {
            await next(context);
        }
        catch (Exception)
        {
            _logger.LogInformation("marking job as failed");
            // we need a clean session
            using var failureScope =  _serviceProvider.CreateScope();
            await using var session = failureScope.ServiceProvider.GetRequiredService<ISession>();
            var job = await session.GetById<Job>(context.JobId)
                      ?? throw new JobNotFoundException(context.JobId);
            
            job.Attempts++;
            job.LastAttempted = SystemDateTime.UtcNow;
            
            if (job.Attempts > job.MaxRetries)
            {
                //we have reached our limit
                job.DeadLettered = true;
                _logger.LogWarning("Dead lettered");
            }
            else
            {
                var exponent = context.Job.Attempts * _random.Next(300, 350);
                var ts = TimeSpan.FromTicks(exponent);
                context.Job.ScheduledFor = SystemDateTime.UtcNow.Add(ts);
                
                _logger.LogWarning("requeue for attempt {number} at {date}", 
                    job.Attempts, 
                    job.ScheduledFor);
            }
            
            //update any un saved changes.
            await session.SaveChanges();
            
            throw;
        }
    }
}

public class LoadJobIntoContextAction<T> : IProcessAction<T> 
{
    readonly ISession _session;
    readonly ILogger<LoadJobIntoContextAction<T>> _logger;

    public LoadJobIntoContextAction(
        ISession session, 
        ILogger<LoadJobIntoContextAction<T>> logger)
    {
        _session = session;
        _logger = logger;
    }
    
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        //load from the database
        var job = await _session.GetById<Job>(context.JobId)
                  ?? throw new JobNotFoundException(context.JobId);
        

        _logger.LogInformation("Processing");
        
        
        context.Job = job;
        context.Payload = JsonSerializer.Deserialize<T>(job.Payload);

        await next(context);
        
        //processed, lets remove
        _session.Delete<Job>(context.JobId);
        _logger.LogInformation("Completed");
        
        //update any un saved changes.
        await _session.SaveChanges();
    }
}

 
public class QueueNextAction<T> : IProcessAction<T>
{
    readonly ISession _session;
    readonly ICrontab _crontab;
    readonly ILogger<QueueNextAction<T>> _logger;

    public QueueNextAction(
        ISession session, 
        ICrontab crontab,
        ILogger<QueueNextAction<T>> logger)
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



public class MinimalAction<T> : IProcessAction<T>
{
    readonly MinimalLambdaHandlerRegistry _minimalLambdaHandlerRegistry;
    readonly MinimalDelegator _minimalDelegator;
    
    public MinimalAction(MinimalLambdaHandlerRegistry minimalLambdaHandlerRegistry, MinimalDelegator minimalDelegator)
    {
        _minimalLambdaHandlerRegistry = minimalLambdaHandlerRegistry;
        _minimalDelegator = minimalDelegator;
    }
    
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {

        var isFullHandler = _minimalLambdaHandlerRegistry.Get<JobContext<T>>() is null;
        if (isFullHandler)
        {
            await next(context);
        }
        else
        {
            await _minimalDelegator.Execute(context);
        }
    }
}


public class HandlerAction<T> : IProcessAction<T>
{
    readonly IJobHandler<T> _handler;

    public HandlerAction(IJobHandler<T> handler) 
    {
        _handler = handler;
    }
    
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        await _handler.Execute(context);
    }
}


public class DefaultCrontab : ICrontab
{
    public DateTime Next(string cronExpression)
    {
        var schedule = CrontabSchedule.Parse(cronExpression);
        var result = schedule.GetNextOccurrence(SystemDateTime.UtcNow);
        return result;
    }
}


public interface ICrontab
{
    DateTime Next(string cronExpression);
} 

public class JobNotFoundException : LatersException
{
    public string JobId { get; }

    public JobNotFoundException(string jobId) : base($"Cannot find {jobId}")
    {
        JobId = jobId;
    }
}

public interface IProcessAction<T> : IAction<JobContext<T>> {}

