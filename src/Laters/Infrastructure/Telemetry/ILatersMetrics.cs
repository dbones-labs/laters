namespace Laters.Infrastructure.Telemetry;

using System.Diagnostics.Metrics;

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
