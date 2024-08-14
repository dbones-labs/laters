namespace Laters.ClientProcessing.Middleware;

using Data;
using Dbones.Pipes;
using Infrastructure;
using Models;

/// <summary>
/// this is the catch all error handler, where we handle if we requeue
/// the job
/// </summary>
/// <typeparam name="T">the job type</typeparam>
public class FailureAction<T> : IProcessAction<T>
{
    static Random _random = new();
    
    readonly IServiceProvider _serviceProvider;
    readonly ILogger<FailureAction<T>> _logger;

    /// <summary>
    /// this is the catch all error handler, where we handle if we requeue
    /// </summary>
    /// <param name="serviceProvider">scoped ioc provider</param>
    /// <param name="logger">well its a logger that we will use</param>
    public FailureAction(
        IServiceProvider serviceProvider,
        ILogger<FailureAction<T>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            await next(context);
        }
        catch (JobNotFoundException)
        {
            //the job has been processed or does not exist
            //there is nothing to process
        }
        catch (Exception)
        {
            _logger.LogInformation("marking job as failed");
            // we need a clean session
            using var failureScope =  _serviceProvider.CreateScope();
            var session = failureScope.ServiceProvider.GetRequiredService<ISession>();
            var job = await session.GetById<Job>(context.JobId)
                      ?? throw new JobNotFoundException(context.JobId);
            
            job.Attempts++;
            job.LastAttempted = SystemDateTime.UtcNow;
            
            if (job.Attempts > job.MaxRetries)
            {
                //we have reached our limit
                job.DeadLettered = true;
                _logger.DeadLettered();
            }
            else
            {
                var exponent = job.Attempts * _random.Next(300, 350);
                var ts = TimeSpan.FromTicks(exponent);
                job.ScheduledFor = SystemDateTime.UtcNow.Add(ts);
                
                _logger.Requeueing(job.Attempts, job.ScheduledFor);
            }
            
            //update any un saved changes.
            await session.SaveChanges();
            
            throw;
        }
    }
}

[LoggerFor(Type = typeof(FailureAction<>), Registry = EventId.FailureActionLogging)]
static partial class FailureActionLogging
{
    [LoggerMessage(201, LogLevel.Warning, "Dead lettered")]
    public static partial void DeadLettered(this ILogger logger);
    
    
    [LoggerMessage(202, LogLevel.Information, "requeue for attempt {number} at {date}")]
    public static partial void Requeueing(this ILogger logger, int number, DateTime date);
}