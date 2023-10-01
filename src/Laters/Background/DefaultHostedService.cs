namespace Laters;

/// <summary>
/// todo register
/// </summary>
public class DefaultHostedService : IHostedService
{
    readonly LeaderElectionService _leaderElectionService;

    public DefaultHostedService(LeaderElectionService leaderElectionService)
    {
        _leaderElectionService = leaderElectionService;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _leaderElectionService.Initialize(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _leaderElectionService.CleanUp(cancellationToken);
    }
}