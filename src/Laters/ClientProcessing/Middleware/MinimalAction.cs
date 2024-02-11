namespace Laters.ClientProcessing.Middleware;

using Mnimal;
using Pipes;

public class MinimalAction<T> : IProcessAction<T>
{
    readonly MinimalLambdaHandlerRegistry _minimalLambdaHandlerRegistry;
    readonly MinimalDelegator _minimalDelegator;
    
    public MinimalAction(MinimalLambdaHandlerRegistry minimalLambdaHandlerRegistry, MinimalDelegator minimalDelegator)
    {
        _minimalLambdaHandlerRegistry = minimalLambdaHandlerRegistry;
        _minimalDelegator = minimalDelegator;
    }
    
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {

        var isFullHandler = _minimalLambdaHandlerRegistry.Get<JobContext<T>>() is null;
        if (isFullHandler)
        {
            await next(context);
        }
        else
        {
            await _minimalDelegator.Execute(context);
        }
    }
}