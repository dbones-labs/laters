namespace Laters.ServerProcessing.Engine;

using ClientProcessing;
using LeaderContext = Laters.ServerProcessing.LeaderContext;

public class WebWorker : IDisposable
{
    readonly JobWorkerQueue _jobWorkerQueue;
    readonly WorkerClient _workerClient;
    readonly LeaderContext _leaderContext;
    readonly ContinuousLambda _lambda;

    public WebWorker(
        JobWorkerQueue jobWorkerQueue,
        WorkerClient workerClient,
        LeaderContext leaderContext
    )
    {
        _jobWorkerQueue = jobWorkerQueue;
        _workerClient = workerClient;
        _leaderContext = leaderContext;

        _lambda = new ContinuousLambda(nameof(SendJobToWorker), async ()=> await SendJobToWorker(), _jobWorkerQueue.NextTrigger, false);
    }

    public Task Initialize(CancellationToken cancellationToken)
    {
        _lambda.Start(cancellationToken);
        return Task.CompletedTask;
    }


    async Task SendJobToWorker()
    {
        var candidate = _jobWorkerQueue.Next();
        
        //we have some sort of race condition
        //(quit out, asap, and see if the queue has more work)
        if (candidate == null)
        {
            return;
        }
        
        var jobToProcess = new ProcessJob(candidate.Id, candidate.JobType, _leaderContext.ServerId);
        await _workerClient.DelegateJob(jobToProcess);
    }


    public void Dispose()
    {
        _lambda.Dispose();
    }
}