namespace Laters.Infrastucture.Telemetry;

using System.Diagnostics.Metrics;

/// <summary>
/// todo register
/// </summary>
public class LatersMetrics
{
    public LatersMetrics()
    {
        Meter = new Meter("laters");
        JobTypeCounter = Meter.CreateCounter<int>("laters_job_counter");
    }
    
    public Meter Meter { get; set; }

    public Counter<int> JobTypeCounter { get; set; }
}