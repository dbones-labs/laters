namespace Laters.Background;

using Configuration;
using Laters.Infrastructure.Telemetry;
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
    readonly JobWorkerQueue _jobWorkerQueue;
    readonly StorageMetricsRunner _storageMetricsRunner;
    readonly EnsureJobInstancesForCron _ensureJobInstancesForCron;
    readonly LatersConfiguration _latersConfiguration;
    readonly ILogger<DefaultHostedService> _logger;

    /// <summary>
    /// creates an instance of the <see cref="DefaultHostedService"/>
    /// </summary>
    /// <param name="leaderElectionService">instance</param>
    /// <param name="defaultTumbler">instance</param>
    /// <param name="jobWorkerQueue">instance</param>
    /// <param name="storageMetricsRunner">instance</param>
    /// <param name="ensureJobInstancesForCron"></param>
    /// <param name="latersConfiguration"></param>
    /// <param name="logger"></param>
    public DefaultHostedService(
        LeaderElectionService leaderElectionService,
        DefaultTumbler defaultTumbler,
        JobWorkerQueue jobWorkerQueue,
        StorageMetricsRunner storageMetricsRunner,
        EnsureJobInstancesForCron ensureJobInstancesForCron,
        LatersConfiguration latersConfiguration,
        ILogger<DefaultHostedService> logger)
    {
        _leaderElectionService = leaderElectionService;
        _defaultTumbler = defaultTumbler;
        _jobWorkerQueue = jobWorkerQueue;
        _storageMetricsRunner = storageMetricsRunner;
        _ensureJobInstancesForCron = ensureJobInstancesForCron;
        _latersConfiguration = latersConfiguration;
        _logger = logger;
    }
    
    ///<inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_latersConfiguration.Role == Roles.Worker) return Task.CompletedTask;
     
        _logger.LogInformation("initializing the server components"); 
        _leaderElectionService.Initialize(cancellationToken);
        _defaultTumbler.Initialize(cancellationToken);
        _jobWorkerQueue.Initialize(cancellationToken);
        _storageMetricsRunner.Initialize(cancellationToken);
        _ensureJobInstancesForCron.Initialize(cancellationToken);
        
         return Task.CompletedTask;
    }

    ///<inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _leaderElectionService.CleanUp(cancellationToken);
    }
}