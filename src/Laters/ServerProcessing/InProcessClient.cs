namespace Laters.ServerProcessing;

using ClientProcessing;

/// <summary>
/// this allows us to process the jobs within the same worker.
/// </summary>
public class InProcessClient : IWorkerClient
{
    readonly MiddlewareDelegateFactory _middlewareDelegateFactory;
    readonly IServiceProvider _serviceProvider;
    
    /// <summary>
    /// create a new instance of <see cref="InProcessClient"/>
    /// </summary>
    /// <param name="middlewareDelegateFactory">the client middleware pipeline</param>
    /// <param name="serviceProvider">the ioc container, which will resolve dependencies from</param>
    public InProcessClient(MiddlewareDelegateFactory middlewareDelegateFactory, IServiceProvider serviceProvider)
    {
        _middlewareDelegateFactory = middlewareDelegateFactory;
        _serviceProvider = serviceProvider;
    }
    
    /// <summary>
    /// process the job in the same worker
    /// </summary>
    /// <param name="processJob">the job to process</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <exception cref="ArgumentNullException">Check for a null on incoming params</exception>
    public async Task DelegateJob(ProcessJob processJob, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processJob);
        
        using var scope = _serviceProvider.CreateScope(); //being overly careful.
        var execute = _middlewareDelegateFactory.GetExecute(processJob.JobType);
        await execute(scope.ServiceProvider, processJob, cancellationToken);
    }
}