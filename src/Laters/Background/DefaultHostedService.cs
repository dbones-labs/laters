namespace Laters;

/// <summary>
/// todo register
/// </summary>
public class DefaultHostedService : IHostedService
{
    readonly ServerService _serverService;

    public DefaultHostedService(ServerService serverService)
    {
        _serverService = serverService;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _serverService.Initialize(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _serverService.CleanUp(cancellationToken);
    }
}