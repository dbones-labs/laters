namespace Laters.Background;

/// <summary>
/// where we setup the global cron jobs at start of the application
/// </summary>
public class GlobalCronSetup : IHostedService
{
    readonly IServiceProvider _serviceProvider;
    readonly GlobalScheduleCronProxy _proxy;

    public GlobalCronSetup(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// setup the global cron jobs
    /// </summary>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>async</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var setups = scope.ServiceProvider.GetServices<ISetupSchedule>();
        var proxy = scope.ServiceProvider.GetRequiredService<GlobalScheduleCronProxy>();

        foreach (var setup in setups)
        {
            setup.Configure(proxy);
        }
        
        await proxy.SaveChanges(cancellationToken);
    }

    /// <summary>
    /// handles any stopping of the cron job setup
    /// </summary>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>async</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}