namespace Laters.ClientProcessing.Middleware;

using System.Text.Json;
using Data;
using Models;
using Pipes;

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