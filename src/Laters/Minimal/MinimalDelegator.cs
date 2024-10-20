namespace Laters.Minimal;

/// <summary>
/// used in the pipeline to provide the correct delegate to execute for a job, using the minimal handler
/// </summary>
public class MinimalDelegator
{
    readonly IServiceProvider _serviceProvider;
    readonly MinimalLambdaHandlerRegistry _minimalLambdaHandlerRegistry;

    public MinimalDelegator(
        IServiceProvider serviceProvider, 
        MinimalLambdaHandlerRegistry minimalLambdaHandlerRegistry)
    {
        _serviceProvider = serviceProvider;
        _minimalLambdaHandlerRegistry = minimalLambdaHandlerRegistry;
    }
    
    public async Task Execute<T>(T payload)
    {
        var minimalHandler = _minimalLambdaHandlerRegistry.Get<T>();
        if (minimalHandler == null)
        {
            throw new NotSupportedException($"no minimal Api for {typeof(T).FullName}");
        }
        await minimalHandler(_serviceProvider, payload);
    }
}