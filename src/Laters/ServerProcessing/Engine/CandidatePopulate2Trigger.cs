namespace Laters.Engine;

/// <summary>
/// this is to control the reader
/// </summary>
public class CandidatePopulateTrigger : ITrigger
{
    private volatile bool _running = false;
    private readonly ManualTrigger _internalIndicatedTrigger = new();
    
    public async Task Wait(CancellationToken cancellationToken)
    {
        await _internalIndicatedTrigger.Wait(cancellationToken);
    }

    public void UpdateFromQueue(int count)
    {
        if (_running && count == 0)
        {
            _running = false;
            _internalIndicatedTrigger.Stop();
        }

        if (!_running && count > 0)
        {
            _running = true;
            _internalIndicatedTrigger.Continue();
        }
    }
}

