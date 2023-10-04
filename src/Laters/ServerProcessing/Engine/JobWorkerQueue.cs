namespace Laters.Engine;

using System.Collections.Concurrent;

public class JobWorkerQueue : IDisposable
{
    //injected
    readonly LeaderContext _leaderContext;
    readonly DefaultTumbler _tumbler;
    readonly IServiceProvider _scope;
    readonly LatersConfiguration _configuration;

    //local state
    ConcurrentQueue<Candidate> _candidates = new();
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
        IServiceProvider scope,
        LatersConfiguration configuration)
    {
        _leaderContext = leaderContext;
        _tumbler = tumbler;
        _scope = scope;
        _configuration = configuration;
        _nextTrigger = new CandidateNextTrigger();
        
        _populateTrigger = new CandidatePopulateTrigger(TimeSpan.FromSeconds(3));

        _populateLambda =
            new ContinuousLambda(async ()=> await PopulateCandidates(), _populateTrigger);
    }
    
    public void Initialize(CancellationToken cancellationToken)
    {
        _populateLambda.Start(cancellationToken);
    }

    /// <summary>
    /// get the next candidate job to process, which has not been rate limited
    /// </summary>
    /// <returns>candidate</returns>
    public virtual Candidate? Next()
    {
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
            }
            else
            {
                //ok lets set this to null, and we will pick this up in the next data read.
                candidate = null;
            }
        }
        
        _nextTrigger.UpdateFromQueue(Count);
        
        return candidate;
    }

    /// <summary>
    /// fill the in memory queue with candidates
    /// </summary>
    protected virtual async Task PopulateCandidates()
    {
        //no longer leader
        if (!_leaderContext.IsLeader) return;
        
        using var workingScope = _scope.CreateScope();
        await using var querySession = _scope.GetRequiredService<ISession>();
        var windowNames = _tumbler.GetWindowsWhichAreWithinLimits();

        var take = _configuration.InMemoryWorkerQueueMax; //for now we will populate the queue (fully)
        var candidates = await querySession.GetJobsToProcess(windowNames, 0, take);

        foreach (var candidate in candidates)
        {
            _candidates.Enqueue(candidate);
        }
        
        _nextTrigger.UpdateFromReader(candidates.Count, take);
        _populateTrigger.RetrievedFromDatabase(candidates.Count, take, _configuration.InMemoryWorkerQueueMax);
    }

    public void Dispose()
    {
        _populateLambda.Dispose();
    }
}