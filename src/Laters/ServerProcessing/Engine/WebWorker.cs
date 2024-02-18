namespace Laters.ServerProcessing.Engine;

using System.Diagnostics;
using ClientProcessing;
using Infrastucture.Telemetry;
using LeaderContext = Laters.ServerProcessing.LeaderContext;

public class WebWorker : IDisposable
{
    readonly JobWorkerQueue _jobWorkerQueue;
    readonly WorkerClient _workerClient;
    readonly LeaderContext _leaderContext;
    readonly Telemetry _telemetry;
    readonly ILogger<WebWorker> _logger;
    readonly ContinuousLambda _lambda;

    readonly string _workerId = Guid.NewGuid().ToString("D");

    public WebWorker(
        JobWorkerQueue jobWorkerQueue,
        WorkerClient workerClient,
        LeaderContext leaderContext,
        Telemetry telemetry,
        ILogger<WebWorker> logger)
    {
        _jobWorkerQueue = jobWorkerQueue;
        _workerClient = workerClient;
        _leaderContext = leaderContext;
        _telemetry = telemetry;
        _logger = logger;

        _lambda = new ContinuousLambda(nameof(SendJobToWorker), async ()=> await SendJobToWorker(), _jobWorkerQueue.NextTrigger, false);
    }

    public Task Initialize(CancellationToken cancellationToken)
    {
        _lambda.Start(cancellationToken);
        return Task.CompletedTask;
    }


    async Task SendJobToWorker()
    {
        using var _ = _logger.BeginScope(new Dictionary<string, string>
        {
            { "WorkerId", _workerId },
            { "LeaderId", _leaderContext.ServerId },
            { "Action", nameof(SendJobToWorker) }
        });
        
        using var activity = _telemetry.StartActivity<WebWorker>(ActivityKind.Producer);
        if (activity is not null)
        {
            Activity.Current = activity;
        }
        
        activity?.AddTag("worker.id", _workerId);
        activity?.AddTag("leader.id", _leaderContext.ServerId);
        
        _logger.LogInformation("starting the queueing for worker");
        var candidate = _jobWorkerQueue.Next();
        
        activity.AddEvent(new ActivityEvent(nameof(_jobWorkerQueue.Next)));
        activity?.AddTag("job.id", candidate.Id);
        activity?.AddTag("job.type", candidate.JobType);
        activity?.AddTag("job.windowName", candidate.WindowName);
        
        //we have some sort of race condition
        //(quit out, asap, and see if the queue has more work)
        if (candidate == null)
        {
            _logger.LogDebug("found no work");
            return;
        }
        
        
        
        _logger.LogInformation("sending work jobId {JobId}", candidate.Id);
        var jobToProcess = new ProcessJob(candidate.Id, candidate.JobType, _leaderContext.ServerId);
        await _workerClient.DelegateJob(jobToProcess);
        
        _jobWorkerQueue.MarkAsDone(candidate);
        _logger.LogInformation("completed work");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }


    public void Dispose()
    {
        _lambda.Dispose();
    }
}