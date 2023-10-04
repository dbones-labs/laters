namespace Laters;

/// <summary>
/// this is how we can run a function when a trigger is met
/// </summary>
public class ContinuousLambda : IDisposable
{
    readonly Func<Task> _func;
    readonly ITrigger _trigger;
    bool _initialWait;
    bool _isRunning;
    Task? _backgroundWorker;
    CancellationToken _cancellationToken;

    public ContinuousLambda(Func<Task> func, ITrigger trigger, bool runAtStart = true)
    {
        _func = func;
        _trigger = trigger;
        _initialWait = !runAtStart;
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

        //in a rare case we actually do not want want to do this.
        if (_initialWait)
        {
            await _trigger.Wait(_cancellationToken);
            _initialWait = false;
        }

        while (IsRunning())
        {
            try
            {
                await _func();
                await _trigger.Wait(_cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    public void Dispose()
    {
        if (!_isRunning) return;
        _isRunning = false;
        _backgroundWorker?.Dispose();
    }
}