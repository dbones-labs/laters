namespace Laters.Engine;

public class WebWorker : IDisposable
{
    readonly IJobWorkerQueue _jobWorkerQueue;
    readonly WorkerClient _workerClient;
    readonly ContinuousLambda _lambda;

    public WebWorker(
        IJobWorkerQueue jobWorkerQueue,
        WorkerClient workerClient
    )
    {
        _jobWorkerQueue = jobWorkerQueue;
        _workerClient = workerClient;

        _lambda = new ContinuousLambda(async ()=> await Process(), _jobWorkerQueue.NextTrigger, false);
    }

    public Task Initialize(CancellationToken cancellationToken)
    {
        _lambda.Start(cancellationToken);
        return Task.CompletedTask;
    }


    async Task Process()
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