namespace Laters;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public class Window : INotifyPropertyChanged, IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new();
    private volatile int _count = 0; //cached count value.
    private readonly Dictionary<DateTime, int> _items = new();
    private ContinuousLambda? _cleanup;
    private bool _availableCapacity = false;

    public void Initialize(CancellationToken cancellationToken)
    {
        _cleanup = new ContinuousLambda(CleanUp, new TimeTrigger(CleanUpInterval));
        _cleanup.Start(cancellationToken);
    }

    public bool ReachedMax => !_availableCapacity;
    public bool AvailableCapacity => _availableCapacity;

    /// <summary>
    /// current number of items in the window
    /// </summary>
    public int Count => _count;
    
    /// <summary>
    /// the maximum number of items to process in the window
    /// </summary>
    public int MaxCount { get; set; }
    
    /// <summary>
    /// the size of the window, of which we are throttling with 
    /// </summary>
    public TimeSpan Span { get; set; }

    /// <summary>
    /// we want to break slices, default this is 1/4 seconds
    /// </summary>
    public TimeSpan SlicePrecision { get; set; } = TimeSpan.FromMilliseconds(250);

    public TimeSpan CleanUpInterval { get; set; }

    public void AddItemsToWindow(DateTime slice, int count)
    {
        var windowedSlice = slice.Truncate(SlicePrecision);
        
        _lock.EnterWriteLock();
        try
        {
            var hasSlice = _items.ContainsKey(windowedSlice);
            if (hasSlice)
            {
                _items[windowedSlice] += count;
            }
            else
            {
                _items.Add(windowedSlice, count);
            }

            //long hand, due to volatile.
            _count += count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        UpdateCount();
    }

    private void UpdateCount()
    {
        var newCapacity = _count < MaxCount;
        if (newCapacity != _availableCapacity)
        {
            _availableCapacity = newCapacity;
            OnPropertyChanged(nameof(AvailableCapacity));
            OnPropertyChanged(nameof(ReachedMax));
        }
    }
    
    private Task CleanUp()
    {
        var deleteAllBefore = SystemDateTime
            .UtcNow
            .AddTicks(-Span.Ticks)
            .Truncate(SlicePrecision);
        
        _lock.EnterUpgradeableReadLock();
        try
        {
            //we can read the keys to remove first
            var timesToDelete = _items
                .Keys
                .Where(x => x < deleteAllBefore)
                .ToList();

            _lock.EnterWriteLock();
            try
            {
                //now we have promoted the lock we can remove
                foreach (var time in timesToDelete)
                {
                    //lets remove the time frame
                    _items.Remove(time);
                }

                var total = _items.Values.Sum();
                _count = total;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
        
        UpdateCount();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _lock.Dispose();
        _cleanup.Dispose();
    }
    
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}