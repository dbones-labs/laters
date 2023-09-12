namespace Laters;

using System.Diagnostics;

/// <summary>
/// todo register
/// </summary>
public class Telemetry : IDisposable
{
    public Telemetry()
    {
        ActivitySource = new ActivitySource(Name);
    }

    public static string Header = "open.telemetry";
    public static string Name = "laters.opentelemetry";

    public ActivitySource ActivitySource { get; }

    public void Dispose()
    {
        ActivitySource?.Dispose();
    }
}