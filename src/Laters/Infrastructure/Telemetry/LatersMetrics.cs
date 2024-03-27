namespace Laters.Infrastructure.Telemetry;

using System.Diagnostics.Metrics;


/// <summary>
/// metrics for the laters library
/// </summary>
public class LatersMetrics : ILatersMetrics
{

    public LatersMetrics(IMeterFactory meterFactory, StorageMetricsRunner storageMetricsRunner)
    {
        //counters
        Meter = meterFactory.Create(Traces.Name);
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