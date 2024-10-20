namespace Laters.ServerProcessing;

using Triggers;

/// <summary>
/// this is how we can run a function when a trigger is met
/// </summary>
public class ContinuousLambda : IDisposable
{
    readonly string _name; // for debug purposes
    readonly Func<Task> _func;
    readonly ITrigger _trigger;
    bool _initialWait;
    bool _isRunning;
    Task? _backgroundWorker;
    CancellationToken _cancellationToken;

    /// <summary>
    /// creates a lambda that will run when the trigger is met
    /// </summary>
    /// <param name="name">name of the lamabda, really used for readability</param>
    /// <param name="func">the function which will run when the trigger is invoked</param>
    /// <param name="trigger">used to trigger the passed in lambda</param>
    /// <param name="runAtStart">if to kick off when we start the the lambda, before the trigger is invoked</param>
    public ContinuousLambda(string name, Func<Task> func, ITrigger trigger, bool runAtStart = true)
    {
        _name = name;
        _func = func;
        _trigger = trigger;
        _initialWait = !runAtStart;
    }

    /// <summary>
    /// this will start the lambda running
    /// </summary>
    /// <param name="cancellationToken">this will stop the lambda loop</param>
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
          
            await _func();
            await _trigger.Wait(_cancellationToken);
        }
    }

    /// <summary>
    /// clean up
    /// </summary>
    public void Dispose()
    {
        if (!_isRunning) return;
        _isRunning = false;
        //_backgroundWorker?.Dispose(); seems the cancel token as done its work
    }
}