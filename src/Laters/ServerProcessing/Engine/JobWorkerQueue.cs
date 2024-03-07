namespace Laters.ServerProcessing.Engine;

using System.Diagnostics;
using Windows;
using Configuration;
using Data;
using Infrastucture;
using Infrastucture.Telemetry;

public class JobWorkerQueue2 : IDisposable
{
    //injected
    readonly WebWorker2 _worker;
    readonly LeaderContext _leaderContext;
    readonly DefaultTumbler _tumbler;
    readonly IServiceProvider _serviceProvider;
    readonly LatersConfiguration _configuration;
    readonly Telemetry _telemetry;
    readonly ILogger<JobWorkerQueue2> _logger;

    //local state
    ReaderWriterLockSlim _lock = new();
    CandidatePopulateTrigger _populateTrigger;
    ContinuousLambda _populateLambda;

    public JobWorkerQueue2(
        WebWorker2 worker,
        LeaderContext leaderContext,
        DefaultTumbler tumbler,
        IServiceProvider serviceProvider,
        LatersConfiguration configuration,
        Telemetry telemetry,
        ILogger<JobWorkerQueue2> logger)
    {
        _worker = worker;
        _leaderContext = leaderContext;
        _tumbler = tumbler;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
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
        using var workingScope = _serviceProvider.CreateScope();

        IList<Candidate> candidates;
        await using (var querySession = workingScope.ServiceProvider.GetRequiredService<ISession>())
        {
            var windowNames = _tumbler.GetWindowsWhichAreWithinLimits();

            var take = _configuration.InMemoryWorkerQueueMax; //this is the in memory queue. (batch)
            candidates = await querySession.GetJobsToProcess(windowNames, 0, take);
            _logger.LogInformation("found {num} candiate jobs", candidates.Count);

            var fetch = take > candidates.Count ? FetchStrategy.Wait : FetchStrategy.Continue;
            _populateTrigger.SetWhenToFetch(fetch);
        }

        activity?.AddTag("queued", candidates.Count);
        
        await candidates.ParallelForEachAsync(
            candidate => SendJobToWorker(cancellationToken, candidate, activity),
            _configuration.NumberOfProcessingThreads);
    }

    Task SendJobToWorker(CancellationToken cancellationToken, Candidate candidate, Activity? activity)
    {
        using (var read = _lock.CreateUpgradeableReadLock())
        {
            var noLongerLeader = !_leaderContext.IsLeader;
            var windowMaxedOut = !_tumbler.AreWeOkToProcessThisWindow(candidate.WindowName);

            if (noLongerLeader || windowMaxedOut)
            {
                _logger.LogInformation(
                    "candidate {num} will be processed later, as the window has reached its limit", candidate.Id);
                return Task.CompletedTask;
            }

            using (var _ = read.EnterWrite())
            {
                //we cannot process this job at this time as we have hit the limit.
                //update the windows!
                _tumbler.RecordJobQueue(candidate.WindowName);
            }
            
            return _worker.SendJobToWorker(candidate, activity.Id, cancellationToken);
        }
    }

    public void Dispose()
    {
        _populateLambda.Dispose();
    }
}