namespace Laters.Engine;

/// <summary>
/// 
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
    private readonly JobWorkerQueue _jobWorkerQueue;
    private readonly List<WebWorker> _workers = new();
    
    public WorkerEngine(JobWorkerQueue jobWorkerQueue, LatersConfiguration latersConfiguration, IServiceProvider provider)
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
        foreach (var worker in _workers)
        {
            await worker.Initialize(cancellationToken);
        }
    }
}