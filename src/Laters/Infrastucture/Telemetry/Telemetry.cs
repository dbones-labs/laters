namespace Laters.Infrastucture.Telemetry;

using System.Diagnostics;

/// <summary>
/// todo register
/// </summary>
public class Telemetry : IDisposable
{
    public static string Header = "open.telemetry";
    public static string Name = "laters.opentelemetry";

    public ActivitySource ActivitySource { get; } = new(Name);

    public void Dispose()
    {
        ActivitySource?.Dispose();
    }
}