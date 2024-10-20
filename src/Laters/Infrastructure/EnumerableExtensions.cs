namespace Laters.Infrastructure;

using System.Collections.Concurrent;

public static class EnumerableExtensions
{
    
    /// <summary>
    /// process the items against the provided function in parallel
    /// </summary>
    /// <param name="source">the list of items</param>
    /// <param name="func">the applied function</param>
    /// <param name="maxDegreeOfParallelism">the number of max threads</param>
    /// <typeparam name="T">type of items</typeparam>
    /// <returns>the task to await on</returns>
    /// <remarks>
    /// https://stackoverflow.com/a/52973907
    /// </remarks>
    public static async Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> func, int maxDegreeOfParallelism = 4)
    {
        await Task.WhenAll(
            Partitioner
                .Create(source)
                .GetPartitions(maxDegreeOfParallelism)
                .AsParallel()
                .Select(p => AwaitPartition(p, func)));
    }
    
    static async Task AwaitPartition<T>(IEnumerator<T> partition, Func<T, Task> func)
    {
        using (partition)
        {
            while (partition.MoveNext())
            {
                await Task.Yield(); // prevents a sync/hot thread hangup
                await func(partition.Current);
            }
        }
    }
}