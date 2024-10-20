namespace Laters.ClientProcessing.Middleware;

using Dbones.Pipes;

/// <summary>
/// used to execute any <see cref="IJobHandler{T}"/> provided by the client code
/// </summary>
/// <typeparam name="T">the job type</typeparam>
public class HandlerAction<T> : IProcessAction<T>
{
    readonly IJobHandler<T> _handler;

    /// <summary>
    /// creates a new instance of <see cref="HandlerAction{T}"/>
    /// </summary>
    /// <param name="handler">the job handler supplied by the application</param>
    public HandlerAction(IJobHandler<T> handler) 
    {
        _handler = handler;
    }
    
    /// <inheritdoc />
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        context.CancellationToken.ThrowIfCancellationRequested(); 
        await _handler.Execute(context);
    }
}
