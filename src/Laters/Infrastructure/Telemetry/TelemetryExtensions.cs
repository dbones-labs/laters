using Laters.Infrastructure.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;


/// <summary>
///  this will allow you to add telemetry to the telemetry builders
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Add laters instrumentation to the tracer provider builder
    /// </summary>
    public static TracerProviderBuilder AddLatersInstrumentation(this TracerProviderBuilder builder)
    {
        builder.AddSource(Telemetry.Name);
        return builder;
    }


    /// <summary>
    /// Add laters instrumentation to the meter provider builder
    /// </summary>
    public static MeterProviderBuilder AddLatersInstrumentation(this MeterProviderBuilder builder)
    {
        builder.AddMeter(Telemetry.Name);
        return builder;
    }
}