namespace Laters.ServerProcessing.Engine;

using System.Collections.Concurrent;
using Windows;
using Configuration;
using Data;
using Triggers;

public class JobWorkerQueue : IDisposable
{
    //injected
    readonly LeaderContext _leaderContext;
    readonly DefaultTumbler _tumbler;
    readonly IServiceProvider _serviceProvider;
    readonly LatersConfiguration _configuration;
    readonly ILogger<JobWorkerQueue> _logger;

    //local state
    ReaderWriterLockSlim _lock = new();
    Queue<Candidate> _candidates = new();
    HashSet<Candidate> _inProcess = new();
    CandidatePopulateTrigger _populateTrigger;
    CandidateNextTrigger _nextTrigger;
    ContinuousLambda _populateLambda;

    /// <summary>
    /// the mechanism to inform web workers when there are items in the queue
    /// to be processed
    /// </summary>
    public ITrigger NextTrigger => _nextTrigger;

    /// <summary>
    /// the number of item in the in memory queue
    /// </summary>
    public int Count => _candidates.Count;

    public JobWorkerQueue(
        LeaderContext leaderContext,
        DefaultTumbler tumbler,
        IServiceProvider serviceProvider,
        LatersConfiguration configuration,
        ILogger<JobWorkerQueue> logger)
    {
        _leaderContext = leaderContext;
        _tumbler = tumbler;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _nextTrigger = new CandidateNextTrigger();

        _populateTrigger = new CandidatePopulateTrigger(TimeSpan.FromSeconds(3), logger);

        _populateLambda =
            new ContinuousLambda(nameof(PopulateCandidates), async () => await PopulateCandidates(), _populateTrigger);
    }

    public void Initialize(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initialize the JobWorkerQueue component");
        _populateLambda.Start(cancellationToken);
    }

    public virtual void MarkAsDone(Candidate? candidate)
    {
        if (candidate is null) return;
        using var writeLock = _lock.CreateWriteLock();
        _inProcess.Remove(candidate);
    }

    /// <summary>
    /// get the next candidate job to process, which has not been rate limited
    /// </summary>
    /// <returns>candidate</returns>
    public virtual Candidate? Next()
    {
        //need to ensure we are thread safe
        using var locked = _lock.CreateWriteLock();
        
        //no longer leader.
        if (!_leaderContext.IsLeader)
        {
            _candidates.Clear();
            return null;
        }

        Candidate? candidate = null;
        if (_candidates.TryDequeue(out candidate))
        {
            //confirm this window is still open
            if (_tumbler.AreWeOkToProcessThisWindow(candidate.WindowName))
            {
                //update the windows!
                _tumbler.RecordJobQueue(candidate.WindowName);
                //update the candidate as in progress
                _inProcess.Add(candidate);
            }
            else
            {
                //ok lets set this to null, and we will pick this up in the next data read.
                candidate = null;
            }
        }

        _nextTrigger.UpdateFromQueue(Count);
        _populateTrigger.UpdateFromQueue(Count);

        return candidate;
    }

    /// <summary>
    /// fill the in memory queue with candidates
    /// </summary>
    protected virtual async Task PopulateCandidates()
    {
        //no longer leader
        if (!_leaderContext.IsLeader) return;

        using var readLock = _lock.CreateUpgradeableReadLock();
        using var workingScope = _serviceProvider.CreateScope();
        await using var querySession = workingScope.ServiceProvider.GetRequiredService<ISession>();
        var windowNames = _tumbler.GetWindowsWhichAreWithinLimits();

        var take = _configuration.InMemoryWorkerQueueMax; //for now we will populate the queue (fully)
        var inProcessIds = _inProcess.Select(x => x.Id).ToList();
        var candidates = await querySession.GetJobsToProcess(inProcessIds, windowNames, 0, take);

        using (var writeLock = readLock.EnterWrite())
        {
            foreach (var candidate in candidates)
            {
                _candidates.Enqueue(candidate);
                _inProcess.Add(candidate);
            }
        }

        _logger.LogInformation("found {num} candiate jobs", candidates.Count);
        _nextTrigger.UpdateFromReader(candidates.Count, take);
        _populateTrigger.RetrievedFromDatabase(candidates.Count, take);
    }

    public void Dispose()
    {
        _populateLambda.Dispose();
    }
}