namespace Laters.ClientProcessing.Middleware;

using Pipes;

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