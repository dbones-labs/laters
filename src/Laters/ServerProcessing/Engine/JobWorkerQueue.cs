namespace Laters.Engine;

using System.Collections.Concurrent;

public class JobWorkerQueue : IJobWorkerQueue, IDisposable
{
    //state
    private ConcurrentQueue<Candidate> _candidates = new();
    
    //injected
    private readonly DefaultTumbler _tumbler;
    private readonly IServiceProvider _scope;
    private readonly LatersConfiguration _configuration;

    //local
    private CandidateNextTrigger _nextTrigger;
    private ContinuousLambda _populateLambda;

    public ITrigger NextTrigger => _nextTrigger;

    public int Count => _candidates.Count;
    
    public JobWorkerQueue(
        DefaultTumbler tumbler,
        IServiceProvider scope,
        LatersConfiguration configuration)
    {
        _tumbler = tumbler;
        _scope = scope;
        _configuration = configuration;
        _nextTrigger = new CandidateNextTrigger();
        var populateTrigger = new CandidatePopulateTrigger();

        _populateLambda =
            new ContinuousLambda(PopulateCandidates, populateTrigger);
    }

    public virtual Candidate? Next()
    {
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

    protected virtual async Task PopulateCandidates()
    {
        using var workingScope = _scope.CreateScope();
        await using var querySession = _scope.GetRequiredService<ISession>();
        var windowNames = _tumbler.GetWindowsWhichAreWithinLimits();

        var pageSize = (int)_configuration.InMemoryWorkerQueueMax / 3;
        var candidates = await querySession.GetJobsToProcess(windowNames, pageSize);

        foreach (var candidate in candidates)
        {
            _candidates.Enqueue(candidate);
        }
        
        _nextTrigger.UpdateFromReader(candidates.Count, pageSize);
    }

    public void Dispose()
    {
        _populateLambda.Dispose();
    }
}

public interface IJobWorkerQueue
{
    Candidate? Next();
    ITrigger NextTrigger { get; }
}