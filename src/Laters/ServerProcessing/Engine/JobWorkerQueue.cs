namespace Laters.ServerProcessing.Engine;

using System.Diagnostics;
using Windows;
using ClientProcessing;
using Configuration;
using Data;
using Infrastructure;
using Infrastructure.Telemetry;

public class JobWorkerQueue : IDisposable
{
    //injected
    readonly LeaderContext _leaderContext;
    readonly DefaultTumbler _tumbler;
    readonly IServiceProvider _serviceProvider;
    readonly LatersConfiguration _configuration;
    readonly WorkerClient _workerClient;
    readonly Traces _telemetry;
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
        Traces telemetry,
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

        //using var activity = _telemetry.StartActivity(nameof(PopulateCandidates), ActivityKind.Internal);

        IList<Candidate> candidates;
        using (var workingScope = _serviceProvider.CreateScope())
        {
            var querySession = workingScope.ServiceProvider.GetRequiredService<ISession>();
            var windowNames = _tumbler.GetWindowsWhichAreWithinLimits();

            var take = _configuration.InMemoryWorkerQueueMax; //this is the in memory queue. (batch)
            candidates = await querySession.GetJobsToProcess(windowNames, 0, take);
            _logger.LogInformation("found {num} candidate jobs", candidates.Count);

            var fetch = take > candidates.Count ? FetchStrategy.Wait : FetchStrategy.Continue;
            _populateTrigger.SetWhenToFetch(fetch);
        }

        //activity?.AddTag("queued", candidates.Count);

        await candidates.ParallelForEachAsync(
            candidate => SendJobToWorker(cancellationToken, candidate),
            _configuration.NumberOfProcessingThreads);
    }

    static object _lock2 = new object();

    async Task SendJobToWorker(CancellationToken cancellationToken, Candidate candidate)
    {
        using var _ = _logger.BeginScope(new Dictionary<string, string>
        {
            { Telemetry.LeaderId, _leaderContext.ServerId },
            { Telemetry.Action, nameof(SendJobToWorker) },
            { Telemetry.JobId, candidate.Id },
            { Telemetry.JobType, candidate.JobType },
            { Telemetry.Window, candidate.WindowName },
            { Telemetry.TraceId, candidate.TraceId ?? "" }
        });

        var noLongerLeader = !_leaderContext.IsLeader;
        var windowMaxedOut = !_tumbler.AreWeOkToProcessThisWindow(candidate.WindowName);

        if (noLongerLeader)
        {
            //this should be rare, but we needed to check
            _logger.LogInformation(
                "candidate {jobId} will be processed later, as node is no longer leader", candidate.Id);
            return;
        }

        if (windowMaxedOut)
        {
            //try and exit out asap. if we cannot process the item.
            _logger.LogInformation(
                "candidate {jobId} will be processed later, as the window has reached its limit", candidate.Id);
            return;
        }

        lock (_lock2)
        {
            //confirm one more time (as we have a full lock)
            windowMaxedOut = !_tumbler.AreWeOkToProcessThisWindow(candidate.WindowName);
            if (windowMaxedOut)
            {            
               //we cannot process this job at this time as we have hit the limit.
                return;
            }

            //update the windows!
            _tumbler.RecordJobQueue(candidate.WindowName);
        }

        using var activity = _telemetry.StartActivity(nameof(SendJobToWorker), ActivityKind.Internal, candidate.TraceId);
        if (activity != null)
        {
            Activity.Current = activity;
            activity.AddTag(Telemetry.LeaderId, _leaderContext.ServerId);
            activity.AddTag(Telemetry.JobId, candidate.Id);
            activity.AddTag(Telemetry.JobType, candidate.JobType);
            activity.AddTag(Telemetry.Window, candidate.WindowName);
        }

        _logger.LogInformation("sending job {num} to be processed", candidate.Id);
        var jobToProcess = new ProcessJob(candidate.Id, candidate.JobType, candidate.WindowName, _leaderContext.ServerId);
        await _workerClient.DelegateJob(jobToProcess, cancellationToken);

        _logger.LogInformation("completed work");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _populateLambda.Dispose();
    }
}