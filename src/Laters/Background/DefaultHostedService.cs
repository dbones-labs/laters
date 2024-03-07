namespace Laters.Background;

using Configuration;
using ServerProcessing;
using ServerProcessing.Engine;
using ServerProcessing.Windows;

/// <summary>
/// all the background processes are started here.
/// </summary>
public class DefaultHostedService : IHostedService
{
    readonly LeaderElectionService _leaderElectionService;
    readonly DefaultTumbler _defaultTumbler;
    readonly JobWorkerQueue2 _jobWorkerQueue;
    readonly LatersConfiguration _latersConfiguration;
    readonly ILogger<DefaultHostedService> _logger;

    public DefaultHostedService(
        LeaderElectionService leaderElectionService,
        DefaultTumbler defaultTumbler,
        JobWorkerQueue2 jobWorkerQueue, 
        LatersConfiguration latersConfiguration,
        ILogger<DefaultHostedService> logger)
    {
        _leaderElectionService = leaderElectionService;
        _defaultTumbler = defaultTumbler;
        _jobWorkerQueue = jobWorkerQueue;
        _latersConfiguration = latersConfiguration;
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_latersConfiguration.Role == Roles.Worker) return;
     
        _logger.LogInformation("initializing the server components"); 
        _leaderElectionService.Initialize(cancellationToken);
        _defaultTumbler.Initialize(cancellationToken);
        _jobWorkerQueue.Initialize(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _leaderElectionService.CleanUp(cancellationToken);
    }
}