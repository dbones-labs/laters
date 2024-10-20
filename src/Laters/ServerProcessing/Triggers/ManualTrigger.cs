namespace Laters.ServerProcessing.Triggers;

public class ManualTrigger : ITrigger, IDisposable
{
    private readonly ManualResetEventSlim _manualEvent = new();

    public ManualTrigger()
    {
        Stop();
    }
    
    public bool IsRunning { get; set; }
    
    public Task Wait(CancellationToken cancellationToken)
    {
        _manualEvent.Wait(cancellationToken);
        return Task.CompletedTask;
    }

    public void Stop()
    {
        IsRunning = false;
        _manualEvent.Reset();
    }
    
    public void Continue()
    {
        IsRunning = true;
        _manualEvent.Set();
    }

    public void Dispose()
    {
        _manualEvent.Dispose();
    }
}