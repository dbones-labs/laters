namespace Laters.ClientProcessing;

using Middleware;
using Pipes;

/// <summary>
/// this is to process the Job
/// </summary>
public class ProcessJobMiddleware<T> : IProcessJobMiddleware<T>, IMiddleware<JobContext<T>>
{
    readonly Middleware<JobContext<T>> _internalMiddleware;

    public ProcessJobMiddleware(ClientActions clientActions)
    {
        _internalMiddleware = new Middleware<JobContext<T>>();
        
        // _internalMiddleware.Add(MakeGeneric<T>(typeof(OpenTelemetryProcessAction<>)));
        _internalMiddleware.Add(MakeGeneric<T>(clientActions.FailureAction));
        _internalMiddleware.Add(MakeGeneric<T>(clientActions.LoadJobIntoContextAction));
        _internalMiddleware.Add(MakeGeneric<T>(clientActions.QueueNextAction));

        foreach (var customActionType in clientActions.CustomActions)
        {
            _internalMiddleware.Add(MakeGeneric<T>(customActionType));
        }
        
        _internalMiddleware.Add(MakeGeneric<T>(typeof(MinimalAction<>)));
        _internalMiddleware.Add(MakeGeneric<T>(clientActions.MainAction));
        
        //we have loaded from the dataabse
        //Otel
        //custom processing
        //Ijobhandler

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