namespace Laters.ServerProcessing.Engine;

using Triggers;

/// <summary>
/// this is to control the reader
/// </summary>
public class CandidatePopulateTrigger : ITrigger
{
    readonly ILogger _logger;
    volatile bool _processing;
    volatile bool _newlyEmpty = true;
    readonly TimeTrigger _internalTimeoutTrigger;
    readonly ManualTrigger _manualTrigger = new ();

    public CandidatePopulateTrigger(TimeSpan waitTime, ILogger logger)
    {
        _logger = logger;
        //if empty scan every 3 seconds
        //newly empty then trigger
        _internalTimeoutTrigger = new TimeTrigger(waitTime);

        _manualTrigger.Continue();
    }
    
    public async Task Wait(CancellationToken cancellationToken)
    {
        ITrigger waitWith = !_newlyEmpty && !_processing
            ? _internalTimeoutTrigger //nothing new, lets wait
            : _manualTrigger; // we just finished processing
        
        _logger.LogDebug("using {Waiter} to wait with, empty: {NewlyEmpty}, processing: {Processing}", 
            waitWith.GetType(),
            _newlyEmpty,
            _processing);
        
        await waitWith.Wait(cancellationToken);
        
        _logger.LogDebug("Lets gooo!");
    }

    public void UpdateFromQueue(int count)
    {
        if (count == 0)
        {
            _logger.LogDebug("In memory queue is empty");
            _processing = false;
            _newlyEmpty = true;
            //lets kick off another query
            _manualTrigger.Continue(); 
        }
    }

    public void RetrievedFromDatabase(int candidatesCount, int take)
    {
        _logger.LogDebug("retrieved {Candidates}", candidatesCount);
        _processing = candidatesCount > 0;
        _newlyEmpty = false;
        _manualTrigger.Stop();
    }
}