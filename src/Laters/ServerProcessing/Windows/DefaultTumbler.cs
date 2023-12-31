﻿namespace Laters;

public class DefaultTumbler : IDisposable
{
    private readonly ManualTrigger _trigger = new ();

    private Window _globalWindow = new();
    private Dictionary<string, Window> _namedWindows = new ();

    public DefaultTumbler(LatersConfiguration configuration)
    {
        _globalWindow.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == "AvailableCapacity")
            {
                UpdateTrigger();
            }
        };
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

        names.Add("global");

        var availableWindows = _namedWindows.Where(x => !x.Value.AvailableCapacity).Select(x => x.Key);
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

    private Task UpdateTrigger()
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