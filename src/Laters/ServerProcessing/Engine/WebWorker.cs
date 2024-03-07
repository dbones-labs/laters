namespace Laters.ServerProcessing.Engine;

using System.Diagnostics;
using ClientProcessing;
using Infrastucture.Telemetry;
using LeaderContext = Laters.ServerProcessing.LeaderContext;


public class WebWorker2
{
    readonly WorkerClient _workerClient;
    readonly LeaderContext _leaderContext;
    readonly Telemetry _telemetry;
    readonly ILogger<WebWorker2> _logger;

    public WebWorker2(
        WorkerClient workerClient,
        LeaderContext leaderContext,
        Telemetry telemetry,
        ILogger<WebWorker2> logger)
    {
        _workerClient = workerClient;
        _leaderContext = leaderContext;
        _telemetry = telemetry;
        _logger = logger;
    }


    public async Task SendJobToWorker(Candidate candidate, string traceId, CancellationToken cancellationToken = default)
    {
        using var _ = _logger.BeginScope(new Dictionary<string, string>
        {
            { "LeaderId", _leaderContext.ServerId },
            { "Action", nameof(SendJobToWorker) }
        });
        
        _logger.LogInformation("sending job {num} to be processed", candidate.Id);
        
        
        using var activity = _telemetry.StartActivity(nameof(SendJobToWorker), ActivityKind.Producer, traceId);
        if (activity is not null)
        {
            Activity.Current = activity;
        }
        
        activity?.AddTag("leader.id", _leaderContext.ServerId);
        activity?.AddTag("job.id", candidate.Id);
        activity?.AddTag("job.type", candidate.JobType);
        activity?.AddTag("job.windowName", candidate.WindowName);
        
        _logger.LogInformation("sending work jobId {JobId}", candidate.Id);
        var jobToProcess = new ProcessJob(candidate.Id, candidate.JobType, _leaderContext.ServerId);
        await _workerClient.DelegateJob(jobToProcess, cancellationToken);
        
        
        _logger.LogInformation("completed work");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}