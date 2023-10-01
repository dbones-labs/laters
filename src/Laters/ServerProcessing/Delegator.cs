namespace Laters;

[Obsolete("removing in favor of the engine")]
public class Delegator : IDelegator
{
    readonly IServiceProvider _scope;
    readonly DefaultTumbler _tumbler;
    readonly IWorkerClient _workerClient;
    readonly LatersConfiguration _latersConfiguration;

    public Delegator(
        IServiceProvider serviceProvider, 
        DefaultTumbler tumbler, 
        IWorkerClient workerClient,
        LatersConfiguration latersConfiguration)
    {
        _scope = serviceProvider;
        _tumbler = tumbler;
        _workerClient = workerClient;
        _latersConfiguration = latersConfiguration;
    }
    
    
    // query for types which have not (tumble should provide)
    // query for N items (with the above filter and ordered by datetime of process dt)
    // ship to the output channel
    
    public async Task Execute(CancellationToken cancellationToken)
    {
        //setup our instance
        using var workingScope = _scope.CreateScope();
        await using var querySession = _scope.GetRequiredService<ISession>();
        var windowNames = _tumbler.GetWindowsWhichAreWithinLimits();

        var candiates = await querySession.GetJobsToProcess(windowNames);
        var beingProcessed = candiates.Select(candidate => Send(candidate)).ToArray();
        
        var results = await Task.WhenAll(beingProcessed);

        //as all calls were not throttled we will 
        var streak = results.Any();
        if (!streak)
        {
            //we have processed all the items
            //lets pause
            await Task.Delay(1000);
        }
    }

    async Task<bool> Send(Candidate candidate)
    {
        //confirm this window is still open
        if (!_tumbler.AreWeOkToProcessThisWindow(candidate.WindowName)) return false;
        //update the windows!
        _tumbler.RecordJobQueue(candidate.WindowName);

        var jobToProcess = new ProcessJob { Id = candidate.Id, JobType = candidate.JobType };
        await _workerClient.DelegateJob(jobToProcess);
        return true;
    }
    
}