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
    readonly List<WebWorker> _workers = new();
    
    public WorkerEngine(
        JobWorkerQueue jobWorkerQueue, 
        LatersConfiguration latersConfiguration, 
        IServiceProvider provider)
    {
        _jobWorkerQueue = jobWorkerQueue;
        for (var i = 0; i < latersConfiguration.NumberOfProcessingThreads; i++)
        {
            var worker = provider.GetRequiredService<WebWorker>();
            _workers.Add(worker);
        }
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        var initialisedWorkers = _workers.Select(x => x.Initialize(cancellationToken)).ToArray();
        await Task.WhenAll(initialisedWorkers);
    }
}