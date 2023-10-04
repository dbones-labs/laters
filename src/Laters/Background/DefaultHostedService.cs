namespace Laters;

using Engine;

public class DefaultHostedService : IHostedService
{
    readonly LeaderElectionService _leaderElectionService;
    readonly DefaultTumbler _defaultTumbler;
    readonly JobWorkerQueue _jobWorkerQueue;
    readonly WorkerEngine _workerEngine;
    readonly LatersConfiguration _latersConfiguration;

    public DefaultHostedService(
        LeaderElectionService leaderElectionService,
        DefaultTumbler defaultTumbler,
        JobWorkerQueue jobWorkerQueue, 
        WorkerEngine workerEngine,
        LatersConfiguration latersConfiguration)
    {
        _leaderElectionService = leaderElectionService;
        _defaultTumbler = defaultTumbler;
        _jobWorkerQueue = jobWorkerQueue;
        _workerEngine = workerEngine;
        _latersConfiguration = latersConfiguration;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_latersConfiguration.Role == Roles.Worker) return;
        
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