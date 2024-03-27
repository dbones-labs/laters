namespace Laters.Infrastructure.Telemetry;

using System.Diagnostics.Metrics;
using Laters.Data;
using Laters.ServerProcessing;
using Laters.ServerProcessing.Triggers;


/// <summary>
/// Make use of Laters Metrics
/// </summary>
public interface ILatersMetrics 
{
    /// <summary>
    /// the meter the app uses
    /// </summary>
    Meter Meter { get; }
    
    /// <summary>
    /// <see cref="Meters.Enqueue"/>
    /// </summary>
    Counter<long> EnqueueCounter { get; }
    
    /// <summary>
    /// <see cref="Meters.Process"/>
    /// </summary>
    Counter<long> ProcessCounter { get; }
    
    /// <summary>
    /// <see cref="Meters.ProcessErrors"/>
    /// </summary>
    Counter<long> ProcessErrorsCounter { get; }
    
    /// <summary>
    /// <see cref="Meters.Ready"/>
    /// </summary>
    ObservableGauge<long> ReadyGauge { get; }

    /// <summary>
    /// <see cref="Meters.Scheduled"/>
    /// </summary>
    ObservableGauge<long> ScheduledGauge { get; }

    /// <summary>
    /// <see cref="Meters.Deadletter"/>
    /// </summary>
    ObservableGauge<long> DeadletterGauge { get; }

    /// <summary>
    /// <see cref="Meters.CronScheduled"/>
    /// </summary>
    ObservableGauge<long> CronScheduledGauge { get; }

    /// <summary>
    /// <see cref="Meters.ProcessTime"/>
    /// </summary>
    Histogram<double> ProcessTime { get; }

}


/// <summary>
/// metrics for the laters library
/// </summary>
public class LatersMetrics : ILatersMetrics
{

    public LatersMetrics(IMeterFactory meterFactory, StorageMetricsRunner storageMetricsRunner)
    {
        //counters
        Meter = meterFactory.Create(Telemetry.Name);
        EnqueueCounter = Meter.CreateCounter<long>(Meters.Enqueue, "ea", "number of jobs enqueued");
        ProcessCounter = Meter.CreateCounter<long>(Meters.Process, "ea", "number of jobs processed");
        ProcessErrorsCounter = Meter.CreateCounter<long>(Meters.ProcessErrors, "ea", "number of jobs processed with errors");

        //gauges
        ReadyGauge = Meter.CreateObservableGauge<long>(Meters.Ready, () => storageMetricsRunner.Ready, "ea", "number of jobs ready to be processed");
        ScheduledGauge = Meter.CreateObservableGauge<long>(Meters.Scheduled, () => storageMetricsRunner.Scheduled, "ea", "number of jobs scheduled to be processed");
        DeadletterGauge = Meter.CreateObservableGauge<long>(Meters.Deadletter, () => storageMetricsRunner.Deadlettered, "ea", "number of jobs deadlettered");
        
        //histograms
        ProcessTime = Meter.CreateHistogram<double>(Meters.ProcessTime, "ms", "time to process the job");
    }
    
    /// <summary>
    /// the meter the app uses
    /// </summary>
    public Meter Meter { get; set; }
    
    /// <summary>
    /// <see cref="Meters.Enqueue"/>
    /// </summary>
    public Counter<long> EnqueueCounter { get; }
    
    /// <summary>
    /// <see cref="Meters.Process"/>
    /// </summary>
    public Counter<long> ProcessCounter { get; }
    
    /// <summary>
    /// <see cref="Meters.ProcessErrors"/>
    /// </summary>
    public Counter<long> ProcessErrorsCounter { get; }
    
    /// <summary>
    /// <see cref="Meters.Ready"/>
    /// </summary>
    public ObservableGauge<long> ReadyGauge { get; }

    /// <summary>
    /// <see cref="Meters.Scheduled"/>
    /// </summary>
    public ObservableGauge<long> ScheduledGauge { get; }

    /// <summary>
    /// <see cref="Meters.Deadletter"/>
    /// </summary>
    public ObservableGauge<long> DeadletterGauge { get; }

    /// <summary>
    /// <see cref="Meters.CronScheduled"/>
    /// </summary>
    public ObservableGauge<long> CronScheduledGauge { get; }

    /// <summary>
    /// <see cref="Meters.ProcessTime"/>
    /// </summary>
    public Histogram<double> ProcessTime { get; }

}



/// <summary>
/// meter names
/// </summary>
public static class Meters
{
    /// <summary>
    /// when a job is enqueued
    /// </summary>
    public static string Enqueue = "laters.jobs.enqueue";

    /// <summary>
    /// when a job is processed
    /// </summary>
    public static string Process = "laters.jobs.process";

    /// <summary>
    /// when a job is processed with errors
    /// </summary>
    public static string ProcessErrors = "laters.jobs.process.errors";

    /// <summary>
    /// number of jobs that are ready to be processed (total/ guage)
    /// </summary>
    public static string Ready = "laters.jobs.ready";

    /// <summary>
    /// number of jobs that are scheduled to be processed (total / guage)
    /// </summary>
    public static string Scheduled = "laters.jobs.scheduled";

    /// <summary>
    /// number of jobs that are deadlettered (total / guage)
    /// </summary>
    public static string Deadletter = "laters.jobs.deadletter";
    
    /// <summary>
    /// number of cron jobs that are scheduled (total / guage)
    /// </summary>?
    public static string CronScheduled = "laters.cron_jobs.scheduled";

        /// <summary>
    /// time to process the message
    /// </summary>
    public static string ProcessTime = "laters.process_time";

    /// <summary>
    /// the type of the job (label)
    /// </summary>
    public static string JobType = "laters.job_type";

    /// <summary>
    /// the window in which the job was processed in (label)
    /// </summary>
    public static string Window = "laters.window";

}


/// <summary>
/// we will use this class to control how we get the metrics from the storage
/// </summary>
public class StorageMetricsRunner 
{
    readonly IServiceProvider _serviceProvider;
    readonly ContinuousLambda _populateCountersLambda;
    readonly ILogger<StorageMetricsRunner> _logger;
    CancellationToken _token = default;

    /// <summary>
    /// create a new instance of the <see cref="StorageMetricsRunner"/>
    /// </summary>
    /// <param name="serviceProvider">service provider</param>
    /// <param name="logger">the logger</param>
    public StorageMetricsRunner(IServiceProvider serviceProvider, ILogger<StorageMetricsRunner> logger)
    {
        var getMetricsTrigger = new TimeTrigger(TimeSpan.FromSeconds(5));

        _serviceProvider = serviceProvider;
        _populateCountersLambda = new ContinuousLambda(nameof(Scan), async() => await Scan(), getMetricsTrigger);
       _logger = logger;

       Ready = new List<Measurement<long>>();
       Scheduled = new List<Measurement<long>>();
       Deadlettered = new List<Measurement<long>>();
    }

    public void Initialize(CancellationToken token) 
    {
        _logger.LogInformation($"Initialize the {nameof(StorageMetricsRunner)} component");
        _populateCountersLambda.Start(token);  
        _token = token;
    }

    public async Task Scan()
    {
        using var scope = _serviceProvider.CreateScope();
        ITelemetrySession session = scope.ServiceProvider.GetRequiredService<ITelemetrySession>();

        var ready = await session.GetReadyJobs();
        var scheduled = await session.GetScheduledJobs();
        var deadlettered = await session.GetDeadletterJobs();

        Ready = ready.Select(x => new Measurement<long>(x.Count, new KeyValuePair<string, object?>[] { new (Meters.JobType, x.Type) })).ToList();
        Scheduled = scheduled.Select(x => new Measurement<long>(x.Count, new KeyValuePair<string, object?>[] { new (Meters.JobType, x.Type) })).ToList();   
        Deadlettered = deadlettered.Select(x => new Measurement<long>(x.Count, new KeyValuePair<string, object?>[] { new (Meters.JobType, x.Type) })).ToList(); 
    }

    /// <summary>
    /// <see cref="Meters.Ready"/>
    /// </summary>
    public List<Measurement<long>> Ready { get; private set; } 

    /// <summary>
    /// <see cref="Meters.Scheduled"/>
    /// </summary>
    public List<Measurement<long>> Scheduled { get; private set; } 

    /// <summary>
    /// <see cref="Meters.Deadletter"/>
    /// </summary>
    public List<Measurement<long>> Deadlettered { get; private set; }

} 