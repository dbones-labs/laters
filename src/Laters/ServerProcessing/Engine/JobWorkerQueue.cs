namespace Laters.ServerProcessing.Engine;

using System.Diagnostics;
using Windows;
using ClientProcessing;
using Configuration;
using Data;
using Infrastucture;
using Infrastucture.Telemetry;

public class JobWorkerQueue : IDisposable
{
    //injected
    readonly LeaderContext _leaderContext;
    readonly DefaultTumbler _tumbler;
    readonly IServiceProvider _serviceProvider;
    readonly LatersConfiguration _configuration;
    readonly WorkerClient _workerClient;
    readonly Telemetry _telemetry;
    readonly ILogger<JobWorkerQueue> _logger;

    //local state
    ReaderWriterLockSlim _lock = new();
    CandidatePopulateTrigger _populateTrigger;
    ContinuousLambda _populateLambda;

    public JobWorkerQueue(
        LeaderContext leaderContext,
        DefaultTumbler tumbler,
        IServiceProvider serviceProvider,
        LatersConfiguration configuration,
        WorkerClient workerClient,
        Telemetry telemetry,
        ILogger<JobWorkerQueue> logger)
    {
        _leaderContext = leaderContext;
        _tumbler = tumbler;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _workerClient = workerClient;
        _telemetry = telemetry;
        _logger = logger;

        _populateTrigger = new CandidatePopulateTrigger(TimeSpan.FromSeconds(_configuration.CheckDatabaseInSeconds), logger);

        _populateLambda =
            new ContinuousLambda(nameof(PopulateCandidates), async () => await PopulateCandidates(), _populateTrigger);
    }

    public void Initialize(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initialize the JobWorkerQueue component");
        _populateLambda.Start(cancellationToken);
    }

    /// <summary>
    /// LETS GOOO
    /// </summary>
    protected virtual async Task PopulateCandidates(CancellationToken cancellationToken = default)
    {
        //no longer leader
        if (!_leaderContext.IsLeader) return;

        using var activity = _telemetry.StartActivity(nameof(PopulateCandidates), ActivityKind.Internal);
        
        IList<Candidate> candidates;
        using (var workingScope = _serviceProvider.CreateScope())
        {
            var querySession = workingScope.ServiceProvider.GetRequiredService<ISession>();
            var windowNames = _tumbler.GetWindowsWhichAreWithinLimits();

            var take = _configuration.InMemoryWorkerQueueMax; //this is the in memory queue. (batch)
            candidates = await querySession.GetJobsToProcess(windowNames, 0, take);
            _logger.LogInformation("found {num} candiate jobs", candidates.Count);

            var fetch = take > candidates.Count ? FetchStrategy.Wait : FetchStrategy.Continue;
            _populateTrigger.SetWhenToFetch(fetch);
        }

        activity?.AddTag("queued", candidates.Count);
        
        await candidates.ParallelForEachAsync(
            candidate => SendJobToWorker(cancellationToken, candidate, activity.Id),
            _configuration.NumberOfProcessingThreads);
    }

    static object _lock2 = new object();
    
    async Task SendJobToWorker(CancellationToken cancellationToken, Candidate candidate, string? traceId)
    {
        using var __ = _logger.BeginScope(new Dictionary<string, string>
        {
            { "LeaderId", _leaderContext.ServerId },
            { "Action", nameof(SendJobToWorker) }
        });
        
        var noLongerLeader = !_leaderContext.IsLeader;
        var windowMaxedOut = !_tumbler.AreWeOkToProcessThisWindow(candidate.WindowName);

        if (noLongerLeader || windowMaxedOut)
        {
            //try and exit out asap. if we cannot process the item.
            _logger.LogInformation(
                "candidate {num} will be processed later, as the window has reached its limit", candidate.Id);
            return;
        }

        lock (_lock2)
        {
            //confirm one more time (as we have a full lock)
            windowMaxedOut = !_tumbler.AreWeOkToProcessThisWindow(candidate.WindowName);
            if (windowMaxedOut)
            {
                return;
            }
            
            //we cannot process this job at this time as we have hit the limit.
            //update the windows!
            _tumbler.RecordJobQueue(candidate.WindowName);
        }
        
        using var activity = _telemetry.StartActivity(nameof(SendJobToWorker), ActivityKind.Internal, traceId);
        if (activity != null)
        {
            Activity.Current = activity;
        }
        activity?.AddTag("leader.id", _leaderContext.ServerId);
        activity?.AddTag("job.id", candidate.Id);
        activity?.AddTag("job.type", candidate.JobType);
        activity?.AddTag("job.windowName", candidate.WindowName);
        
        _logger.LogInformation("sending job {num} to be processed", candidate.Id);
        
        _logger.LogInformation("sending work jobId {JobId}", candidate.Id);
        var jobToProcess = new ProcessJob(candidate.Id, candidate.JobType, _leaderContext.ServerId);
        await _workerClient.DelegateJob(jobToProcess, cancellationToken);
        
        _logger.LogInformation("completed work");
        
        // await _worker.SendJobToWorker(candidate, activity.Id, cancellationToken);
        
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    public void Dispose()
    {
        _populateLambda.Dispose();
    }
}