namespace Laters.Infrastructure.Telemetry;

using System.Diagnostics.Metrics;


/// <summary>
/// metrics for the laters library
/// </summary>
public class Metrics : IMetrics
{

    /// <summary>
    /// instance of the metrics
    /// </summary>
    /// <param name="meterFactory">.NET factory for Metrics</param>
    /// <param name="storageMetricsRunner">the backing store to use for metrics</param>
    public Metrics(
        IMeterFactory meterFactory, 
        StorageMetricsRunner storageMetricsRunner)
    {
        //counters
        Meter = meterFactory.Create(Telemetry.Name);
        EnqueueCounter = Meter.CreateCounter<long>(Telemetry.Enqueue, "ea", "number of jobs enqueued");
        ProcessCounter = Meter.CreateCounter<long>(Telemetry.Process, "ea", "number of jobs processed");
        ProcessErrorsCounter = Meter.CreateCounter<long>(Telemetry.ProcessErrors, "ea", "number of jobs processed with errors");

        //gauges
        ReadyGauge = Meter.CreateObservableGauge<long>(Telemetry.Ready, () => storageMetricsRunner.Ready, "ea", "number of jobs ready to be processed");
        ScheduledGauge = Meter.CreateObservableGauge<long>(Telemetry.Scheduled, () => storageMetricsRunner.Scheduled, "ea", "number of jobs scheduled to be processed");
        DeadletterGauge = Meter.CreateObservableGauge<long>(Telemetry.Deadletter, () => storageMetricsRunner.Deadlettered, "ea", "number of jobs deadlettered");
        
        //histograms
        ProcessTime = Meter.CreateHistogram<double>(Telemetry.ProcessTime, "ms", "time to process the job");
    }
    
    /// <summary>
    /// the meter the app uses
    /// </summary>
    public Meter Meter { get; set; }
    
    /// <summary>
    /// <see cref="Telemetry.Enqueue"/>
    /// </summary>
    public Counter<long> EnqueueCounter { get; }
    
    /// <summary>
    /// <see cref="Telemetry.Process"/>
    /// </summary>
    public Counter<long> ProcessCounter { get; }
    
    /// <summary>
    /// <see cref="Telemetry.ProcessErrors"/>
    /// </summary>
    public Counter<long> ProcessErrorsCounter { get; }
    
    /// <summary>
    /// <see cref="Telemetry.Ready"/>
    /// </summary>
    public ObservableGauge<long> ReadyGauge { get; }

    /// <summary>
    /// <see cref="Telemetry.Scheduled"/>
    /// </summary>
    public ObservableGauge<long> ScheduledGauge { get; }

    /// <summary>
    /// <see cref="Telemetry.Deadletter"/>
    /// </summary>
    public ObservableGauge<long> DeadletterGauge { get; }

    /// <summary>
    /// <see cref="Telemetry.CronScheduled"/>
    /// </summary>
    public ObservableGauge<long> CronScheduledGauge { get; } = null!;

    /// <summary>
    /// <see cref="Telemetry.ProcessTime"/>
    /// </summary>
    public Histogram<double> ProcessTime { get; }

}