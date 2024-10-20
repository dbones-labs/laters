namespace Laters.ServerProcessing.Windows;

using Configuration;
using Infrastructure;
using Triggers;

public class DefaultTumbler : IDisposable
{
    readonly ManualTrigger _trigger = new ();

    Window _globalWindow = new();
    Dictionary<string, Window> _namedWindows = new ();

    public DefaultTumbler(LatersConfiguration configuration)
    {
        var maxWindowConfig = configuration.Windows[LatersConstants.GlobalTumbler];
        _globalWindow.MaxCount = maxWindowConfig.Max;
        _globalWindow.Span = TimeSpan.FromSeconds(maxWindowConfig.SizeInSeconds);
        
        _globalWindow.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == "AvailableCapacity")
            {
                UpdateTrigger();
            }
        };

        var windowConfigs = configuration.Windows.Where(x => x.Key != LatersConstants.GlobalTumbler);
        foreach (var windowConfig in windowConfigs)
        {
            _namedWindows.TryAdd(windowConfig.Key, new Window()
            {
                MaxCount = windowConfig.Value.Max,
                Span = TimeSpan.FromSeconds(windowConfig.Value.SizeInSeconds)
            });
        }
    }

    public void Initialize(CancellationToken cancellationToken)
    {
        _globalWindow.Initialize(cancellationToken);
        foreach (var namedWindow in _namedWindows)
        {
            namedWindow.Value.Initialize(cancellationToken);
        }
    }

    public bool AreWeOkToProcessThisWindow(string windowName)
    {
        if (_globalWindow.ReachedMax) return false;
        return !_namedWindows.TryGetValue(windowName, out var window) || window.AvailableCapacity;
    }
    
    public List<string> GetWindowsWhichAreWithinLimits()
    {
        var names = new List<string>();

        if (_globalWindow.ReachedMax)
        {
            //the global has been reached
            //just return the empty list.
            return names;
        }

        names.Add(LatersConstants.GlobalTumbler);

        var availableWindows = _namedWindows.Where(x => x.Value.AvailableCapacity).Select(x => x.Key);
        names.AddRange(availableWindows);
        
        return names;
    }
    
    public void RecordJobQueue(string rateName)
    {
        var dateTime = SystemDateTime.UtcNow;
        _globalWindow.AddItemsToWindow(dateTime, 1);
        if (_namedWindows.TryGetValue(rateName, out var window))
        {
            window.AddItemsToWindow(dateTime, 1);
        }
    }

    Task UpdateTrigger()
    {
        var shouldRun = _globalWindow.AvailableCapacity;
        if (_trigger.IsRunning)
        {
            if (!shouldRun)
            {
                _trigger.Stop();
            }
        }
        else
        {
            if (shouldRun)
            {
                _trigger.Continue();
            }
        }
        
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _trigger.Dispose();
        _globalWindow.Dispose();
        foreach (var window in _namedWindows)
        {
            window.Value.Dispose();
        }
    }
}