namespace Laters.ClientProcessing.Middleware;

using Minimal;
using Dbones.Pipes;

/// <summary>
/// this will execute the minimal api job handler logic
/// </summary>
/// <typeparam name="T">the message type </typeparam>
public class MinimalAction<T> : IProcessAction<T>
{
    readonly MinimalDelegator _minimalDelegator;
    
    public MinimalAction(MinimalDelegator minimalDelegator)
    {
        _minimalDelegator = minimalDelegator;
    }
    
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        await _minimalDelegator.Execute(context);
    }
}