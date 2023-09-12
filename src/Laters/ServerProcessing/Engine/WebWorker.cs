namespace Laters.Engine;

public class WebWorker : IDisposable
{
    private readonly IJobWorkerQueue _jobWorkerQueue;
    private readonly WorkerClient _workerClient;
    private readonly ContinuousLambda _lambda;

    public WebWorker(
        IJobWorkerQueue jobWorkerQueue,
        WorkerClient workerClient
    )
    {
        _jobWorkerQueue = jobWorkerQueue;
        _workerClient = workerClient;

        _lambda = new ContinuousLambda(Process, _jobWorkerQueue.NextTrigger);
    }

    public Task Initialize(CancellationToken cancellationToken)
    {
        _lambda.Start(cancellationToken);
        return Task.CompletedTask;
    }


    private async Task Process()
    {
        var candidate = _jobWorkerQueue.Next();
        
        //we have some sort of race condition
        //(quit out, asap, and see if the queue has more work)
        if (candidate == null)
        {
            return;
        }
        
        await _workerClient.DelegateJob(new ProcessJob() { Id = candidate.Id, JobType = candidate.JobType });
    }


    public void Dispose()
    {
        _lambda.Dispose();
    }
}