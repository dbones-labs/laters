namespace Laters.ClientProcessing.Middleware;

using Dbones.Pipes;

/// <summary>
/// used to execute any <see cref="IJobHandler{T}"/> provided by the client code
/// </summary>
/// <typeparam name="T">the job type</typeparam>
public class HandlerAction<T> : IProcessAction<T>
{
    readonly IJobHandler<T> _handler;

    public HandlerAction(IJobHandler<T> handler) 
    {
        _handler = handler;
    }
    
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        await _handler.Execute(context);
    }
}