namespace Laters.ClientProcessing;

using Middleware;
using Minimal;
using Dbones.Pipes;

/// <summary>
/// this is to process the Job
/// </summary>
public class ProcessJobMiddleware<T> : IProcessJobMiddleware<T>, IMiddleware<JobContext<T>>
{
    readonly Middleware<JobContext<T>> _internalMiddleware;

    public ProcessJobMiddleware(ClientActions clientActions, MinimalLambdaHandlerRegistry minimalLambdaHandlerRegistry)
    {
        //we setup a pipeline to process a job
        _internalMiddleware = new Middleware<JobContext<T>>();
        
        //these are actions we need to do
        // _internalMiddleware.Add(MakeGeneric<T>(typeof(OpenTelemetryProcessAction<>)));
        _internalMiddleware.Add(MakeGeneric<T>(clientActions.TraceAction));
        _internalMiddleware.Add(MakeGeneric<T>(clientActions.LoggingAction));
        _internalMiddleware.Add(MakeGeneric<T>(clientActions.MetricsAction));
        
        _internalMiddleware.Add(MakeGeneric<T>(clientActions.FailureAction));
        _internalMiddleware.Add(MakeGeneric<T>(clientActions.PersistenceAction));
        _internalMiddleware.Add(MakeGeneric<T>(clientActions.QueueNextAction));

        //these are actions which people may want to add
        foreach (var customActionType in clientActions.CustomActions)
        {
            _internalMiddleware.Add(MakeGeneric<T>(customActionType));
        }
        
        //lastly is the actual handler logic, which can be provided in 2 ways
        var isFullHandler = minimalLambdaHandlerRegistry.Get<JobContext<T>>() is null;
        var handlerToUse = isFullHandler
            ? MakeGeneric<T>(typeof(HandlerAction<>))
            : MakeGeneric<T>(typeof(MinimalAction<>));
        
        _internalMiddleware.Add(handlerToUse);
    }
    
    
    private static Type MakeGeneric<T>(Type type)
    {
        return type.MakeGenericType(typeof(T));
    }

    public async Task Execute(IServiceProvider scope, JobContext<T> context)
    {
        using var transactionScope = scope.CreateScope();
        await _internalMiddleware.Execute(transactionScope.ServiceProvider, context);
    }
}