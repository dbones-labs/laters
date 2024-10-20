namespace Laters.ServerProcessing.Engine;

using Triggers;

/// <summary>
/// this is to control the reader
/// </summary>
public class CandidatePopulateTrigger : ITrigger
{
    readonly ILogger _logger;
    volatile FetchStrategy _fetchStrategy;
    volatile bool _firstRun = true;
    readonly TimeTrigger _internalTimeoutTrigger;
    readonly TimeTrigger _firstWaitTrigger;
    readonly ManualTrigger _manualTrigger = new ();

    public CandidatePopulateTrigger(TimeSpan waitTime, ILogger logger)
    {
        _logger = logger;
        //if empty scan every 3 seconds
        //newly empty then trigger
        _internalTimeoutTrigger = new TimeTrigger(waitTime);
        _firstWaitTrigger = new TimeTrigger(TimeSpan.FromMilliseconds(250));
        _manualTrigger.Continue();
    }
    
    public async Task Wait(CancellationToken cancellationToken)
    {
        ITrigger waitWith = _fetchStrategy == FetchStrategy.Wait
            ? _internalTimeoutTrigger //nothing new, lets wait
            : _manualTrigger; // we just finished processing (get some more)
        
        //this is only for the 1st start
        if (_firstRun)
        {
            waitWith = _firstWaitTrigger;
        }
        
        _logger.LogDebug("using {Waiter}",waitWith.GetType());
        await waitWith.Wait(cancellationToken);
        _logger.LogDebug("Lets gooo!");
    }


    public void SetWhenToFetch(FetchStrategy fetchStrategy)
    {
        _firstRun = false;
        _fetchStrategy = fetchStrategy;
    } 
}

public enum FetchStrategy
{
    Wait,
    Continue
}