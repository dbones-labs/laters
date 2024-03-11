namespace Laters.Background;

public class GlobalCronSetup : IHostedService
{
    readonly IServiceProvider _serviceProvider;
    readonly GlobalScheduleCronProxy _proxy;

    public GlobalCronSetup(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var setups = scope.ServiceProvider.GetServices<ISetupSchedule>();
        var proxy = scope.ServiceProvider.GetRequiredService<GlobalScheduleCronProxy>();

        foreach (var setup in setups)
        {
            setup.Configure(proxy);
        }
        
        await proxy.SaveChanges();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}