namespace Laters.ServerProcessing.Engine;

using Triggers;

/// <summary>
/// this is to control the workers
/// </summary>
public class CandidateNextTrigger : ITrigger
{
    ManualTrigger _internalIndicatedTrigger = new();
    ITrigger _internalTimeTrigger = new TimeTrigger(TimeSpan.FromSeconds(3));
    ITrigger _triggerInUse;

    int _maximum;

    bool _running = false;
    volatile bool _useTimer = true;
    
    public async Task Wait(CancellationToken cancellationToken)
    {
        var trigger = _useTimer
            ? _internalTimeTrigger
            : _internalIndicatedTrigger;

        await trigger.Wait(cancellationToken);
    }


    /// <summary>
    /// should we update the buffer and when
    /// </summary>
    /// <param name="count"></param>
    public void UpdateFromQueue(int count)
    {
        var maximumMet = count >= _maximum && count != 0;
        var outOfItems = count == 0;

        if (_running)
        {
            //we have a full buffer
            if (maximumMet)
            {
                _internalIndicatedTrigger.Stop();
                return;
            }
            
            // //our buffer is running low on items
            // if (minimumMet)
            // {
            //     _internalIndicatedTrigger.Continue();
            //     return;
            // }

            //is out of items
            if (outOfItems)
            {
                _running = false;
                _triggerInUse = _internalTimeTrigger;
            }
        }
    }

    public void UpdateFromReader(int foundNumberOfItems, int pageSize)
    {
        //no items in the queue, back off.
        //or the we have just emptied the queue
        //reset the trigger to the timer
        if (foundNumberOfItems == 0 || foundNumberOfItems < pageSize)
        {
            if (!_running) return;
            _running = false;
            _triggerInUse = _internalTimeTrigger;
        }

        if (_running) return;
        _running = true;
        _triggerInUse = _internalIndicatedTrigger;
        _internalIndicatedTrigger.Continue();
    }
}