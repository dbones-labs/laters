namespace Laters.ClientProcessing.Middleware;

using System.Text.Json;
using Data;
using Models;
using Dbones.Pipes;
using Infrastucture;

/// <summary>
/// as we only have the id of the job, this will handle the persistance (load and save) of the job processing
/// </summary>
/// <typeparam name="T">the job type</typeparam>
public class PersistenceAction<T> : IProcessAction<T> 
{
    readonly ISession _session;
    readonly ILogger<PersistenceAction<T>> _logger;

    public PersistenceAction(
        ISession session, 
        ILogger<PersistenceAction<T>> logger)
    {
        _session = session;
        _logger = logger;
    }
    
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        //load from the database
        var job = await _session.GetById<Job>(context.JobId)
                  ?? throw new JobNotFoundException(context.JobId);

        _logger.StartedToProcess(job.Id);
        context.Job = job;
        context.Payload = JsonSerializer.Deserialize<T>(job.Payload);

        await next(context);
        
        //processed, lets remove
        _session.Delete<Job>(job.Id);
        
        //update any un saved changes.
        await _session.SaveChanges();
        _logger.FinishedProcessing(job.Id);
    }
}

[LoggerFor(Type = typeof(PersistenceAction<>), Registry = EventId.LoadJobIntoContextActionLogging)]
static partial class LoadJobIntoContextActionLogging
{
    [LoggerMessage(101, LogLevel.Information, "loaded and about to process {jobId}")]
    public static partial void StartedToProcess(this ILogger logger, string jobId);
    
    
    [LoggerMessage(102, LogLevel.Information, "processed and removed {jobId}")]
    public static partial void FinishedProcessing(this ILogger logger, string jobId);
}