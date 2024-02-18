namespace Laters.ServerProcessing.Engine;

using Configuration;

/// <summary>
/// setup the worker pool
/// </summary>
/// <remarks>
/// this will bring the
/// job queue
/// worker threads
///
/// together to process 
/// </remarks>
public class WorkerEngine
{
    readonly JobWorkerQueue _jobWorkerQueue;
    readonly ILogger<WorkerEngine> _logger;
    readonly List<WebWorker> _workers = new();
    
    public WorkerEngine(
        JobWorkerQueue jobWorkerQueue, 
        LatersConfiguration latersConfiguration, 
        IServiceProvider provider,
        ILogger<WorkerEngine> logger)
    {
        _jobWorkerQueue = jobWorkerQueue;
        _logger = logger;
        for (var i = 0; i < latersConfiguration.NumberOfProcessingThreads; i++)
        {
            var worker = provider.GetRequiredService<WebWorker>();
            _workers.Add(worker);
        }
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        var initialisedWorkers = _workers.Select(x => x.Initialize(cancellationToken)).ToArray();
        _logger.LogInformation("Setting up workers ({NumberOfThreads})", initialisedWorkers.Length);
        await Task.WhenAll(initialisedWorkers);
    }
}