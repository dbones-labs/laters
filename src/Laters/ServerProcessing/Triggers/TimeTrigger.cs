namespace Laters.ServerProcessing.Triggers;

public class TimeTrigger : ITrigger
{
    private readonly TimeSpan _waitSpan;

    public TimeTrigger(TimeSpan waitSpan)
    {
        _waitSpan = waitSpan;
    }
    
    public async Task Wait(CancellationToken cancellationToken)
    {
        await Task.Delay(_waitSpan, cancellationToken);
    }
}