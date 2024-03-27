namespace Laters.Infrastructure.Telemetry;

using System.Diagnostics.Metrics;
using Laters.Configuration;
using Laters.Data;
using Laters.ServerProcessing;
using Laters.ServerProcessing.Triggers;

/// <summary>
/// we will use this class to control how we get the metrics from the storage
/// </summary>
public class StorageMetricsRunner 
{
    readonly IServiceProvider _serviceProvider;
    readonly LeaderContext _leaderContext;
    readonly ContinuousLambda _populateCountersLambda;
    readonly ILogger<StorageMetricsRunner> _logger;
    CancellationToken _token = default;

    /// <summary>
    /// create a new instance of the <see cref="StorageMetricsRunner"/>
    /// </summary>
    /// <param name="serviceProvider">service provider</param>
    /// <param name="configuration">laters configuration</param> 
    /// <param name="leaderContext">leader context</param>
    /// <param name="logger">the logger</param>
    public StorageMetricsRunner(
        IServiceProvider serviceProvider, 
        LatersConfiguration configuration,
        LeaderContext leaderContext,
        ILogger<StorageMetricsRunner> logger)
    {
        var getMetricsTrigger = new TimeTrigger(TimeSpan.FromSeconds(configuration.CheckTelemetryInSeconds));

        _serviceProvider = serviceProvider;
        _leaderContext = leaderContext;
        _populateCountersLambda = new ContinuousLambda(nameof(Scan), async() => await Scan(), getMetricsTrigger);
       _logger = logger;

       Ready = new List<Measurement<long>>();
       Scheduled = new List<Measurement<long>>();
       Deadlettered = new List<Measurement<long>>();
    }

    /// <summary>
    /// initialize the component
    /// </summary>
    public void Initialize(CancellationToken token) 
    {
        _logger.LogInformation($"Initialize the {nameof(StorageMetricsRunner)} component");
        _populateCountersLambda.Start(token);  
        _token = token;

        //set some empty lists
        Ready = new List<Measurement<long>>();
        Scheduled = new List<Measurement<long>>();
        Deadlettered = new List<Measurement<long>>();
    }

    /// <summary>
    /// scan the database for the latest metrics
    /// </summary>
    public async Task Scan()
    {
        if(!_leaderContext.IsLeader) 
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        ITelemetrySession session = scope.ServiceProvider.GetRequiredService<ITelemetrySession>();

        var ready = await session.GetReadyJobs();
        var scheduled = await session.GetScheduledJobs();
        var deadlettered = await session.GetDeadletterJobs();

        Ready = ready.Select(x => new Measurement<long>(x.Count, new KeyValuePair<string, object?>[] { new (Telemetry.JobType, x.Type) })).ToList();
        Scheduled = scheduled.Select(x => new Measurement<long>(x.Count, new KeyValuePair<string, object?>[] { new (Telemetry.JobType, x.Type) })).ToList();   
        Deadlettered = deadlettered.Select(x => new Measurement<long>(x.Count, new KeyValuePair<string, object?>[] { new (Telemetry.JobType, x.Type) })).ToList(); 
    }

    /// <summary>
    /// <see cref="Telemetry.Ready"/>
    /// </summary>
    public List<Measurement<long>> Ready { get; private set; } 

    /// <summary>
    /// <see cref="Telemetry.Scheduled"/>
    /// </summary>
    public List<Measurement<long>> Scheduled { get; private set; } 

    /// <summary>
    /// <see cref="Telemetry.Deadletter"/>
    /// </summary>
    public List<Measurement<long>> Deadlettered { get; private set; }

} 