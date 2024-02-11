namespace Laters.ClientProcessing.Middleware;

using Data;
using Models;
using Pipes;

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