namespace Laters.Data.InMemory;

using System.Collections.Concurrent;
using Models;

/// <summary>
/// this is not for production use
/// </summary>
public class InMemoryStore
{
    public static object _lock = new();

    public ConcurrentDictionary<string, Entity> Data { get; } = new();
    
    public void Commit(Action<IDictionary<string, Entity>> commitAction)
    {
        lock (_lock)
        {
            commitAction?.Invoke(Data);    
        }
    }
}