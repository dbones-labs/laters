namespace Laters.Background;

using Configuration;
using ServerProcessing;
using ServerProcessing.Engine;
using ServerProcessing.Windows;

public class DefaultHostedService : IHostedService
{
    readonly LeaderElectionService _leaderElectionService;
    readonly DefaultTumbler _defaultTumbler;
    readonly JobWorkerQueue _jobWorkerQueue;
    readonly WorkerEngine _workerEngine;
    readonly LatersConfiguration _latersConfiguration;
    readonly ILogger<DefaultHostedService> _logger;

    public DefaultHostedService(
        LeaderElectionService leaderElectionService,
        DefaultTumbler defaultTumbler,
        JobWorkerQueue jobWorkerQueue, 
        WorkerEngine workerEngine,
        LatersConfiguration latersConfiguration,
        ILogger<DefaultHostedService> logger)
    {
        _leaderElectionService = leaderElectionService;
        _defaultTumbler = defaultTumbler;
        _jobWorkerQueue = jobWorkerQueue;
        _workerEngine = workerEngine;
        _latersConfiguration = latersConfiguration;
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_latersConfiguration.Role == Roles.Worker) return;
     
        _logger.LogInformation("initializing the server components");
        await _leaderElectionService.Initialize(cancellationToken);
        _defaultTumbler.Initialize(cancellationToken);
        _jobWorkerQueue.Initialize(cancellationToken);
        await _workerEngine.Initialize(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _leaderElectionService.CleanUp(cancellationToken);
    }
}