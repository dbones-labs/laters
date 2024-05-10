namespace Laters.Infrastructure.Telemetry;

using System.Diagnostics.Metrics;

/// <summary>
/// Make use of Laters Metrics
/// </summary>
public interface IMetrics 
{
    /// <summary>
    /// the meter the app uses
    /// </summary>
    Meter Meter { get; }
    
    /// <summary>
    /// <see cref="Telemetry.Enqueue"/>
    /// </summary>
    Counter<long> EnqueueCounter { get; }
    
    /// <summary>
    /// <see cref="Telemetry.Process"/>
    /// </summary>
    Counter<long> ProcessCounter { get; }
    
    /// <summary>
    /// <see cref="Telemetry.ProcessErrors"/>
    /// </summary>
    Counter<long> ProcessErrorsCounter { get; }
    
    /// <summary>
    /// <see cref="Telemetry.Ready"/>
    /// </summary>
    ObservableGauge<long> ReadyGauge { get; }

    /// <summary>
    /// <see cref="Telemetry.Scheduled"/>
    /// </summary>
    ObservableGauge<long> ScheduledGauge { get; }

    /// <summary>
    /// <see cref="Telemetry.Deadletter"/>
    /// </summary>
    ObservableGauge<long> DeadletterGauge { get; }

    /// <summary>
    /// <see cref="Telemetry.CronScheduled"/>
    /// </summary>
    ObservableGauge<long> CronScheduledGauge { get; }

    /// <summary>
    /// <see cref="Telemetry.ProcessTime"/>
    /// </summary>
    Histogram<double> ProcessTime { get; }

}
