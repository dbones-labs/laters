namespace Laters;

public class ContinuousLambda : IDisposable
{
    readonly Func<Task> _func;
    readonly ITrigger _trigger;
    bool _isRunning = false;
    Task? _backgroundWorker;
    CancellationToken _cancellationToken;

    public ContinuousLambda(Func<Task> func, ITrigger trigger)
    {
        _func = func;
        _trigger = trigger;
    }

    public void Start(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _isRunning = true;
        _backgroundWorker = Task.Run(async () => await Do(), cancellationToken);
    }

    async Task Do()
    {
        bool IsRunning() => _isRunning && !_cancellationToken.IsCancellationRequested;

        while (IsRunning())
        {
            await _func();
            await _trigger.Wait(_cancellationToken);
        }
    }

    public void Dispose()
    {
        if (!_isRunning) return;
        _isRunning = false;
        _backgroundWorker?.Dispose();
    }
}