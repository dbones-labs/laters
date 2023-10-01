namespace Laters.Engine;

/// <summary>
/// this is to control the reader
/// </summary>
public class CandidatePopulateTrigger : ITrigger
{
    volatile bool _querying;
    readonly TimeTrigger _internalTimeoutTrigger ;

    public CandidatePopulateTrigger(TimeSpan waitTime)
    {
        _internalTimeoutTrigger = new TimeTrigger(waitTime);
    }
    
    public async Task Wait(CancellationToken cancellationToken)
    {
        //its querying
        if (_querying)
        {
            return;
        }
        await _internalTimeoutTrigger.Wait(cancellationToken);
    }
    
    

    public void RetrievedFromDatabase(int count, int pageSize, int max)
    {
        //if empty scan every 3 seconds
        //if triggered, scan until full or non to process

        var noMoreInData = count < pageSize;
        var inMemQueueMaxed = count >= max;
        if (noMoreInData || inMemQueueMaxed)
        {
            _querying = false;
            return;
        }

        var itemsInData = count > 0;
        var spaceForItems = count < max;
        if (itemsInData && spaceForItems)
        {
            _querying = true;
            return;
        }

        _querying = false;
    }
}

