namespace Laters.Infrastructure.Telemetry;

using System.Diagnostics;

/// <summary>
/// todo register
/// </summary>
public class Telemetry : IDisposable
{
    public static string Name = "laters.opentelemetry";

    public ActivitySource ActivitySource { get; } = new(Name);

    public void Dispose()
    {
        ActivitySource?.Dispose();
    }

    public Activity? StartActivity<T>(ActivityKind kind, string? parentId = null)
    {
        return StartActivity(typeof(T).FullName!, kind, parentId);
    }
    
    public Activity? StartActivity(string name, ActivityKind kind, string? parentId = null)
    {
        //try and find the parentId
        if (parentId is null)
        {
            var parent = Activity.Current;

            if (parent != null && !string.IsNullOrEmpty(parent.Id) && parent.IdFormat == ActivityIdFormat.W3C)
            {
                parentId = parent.Id;
            }
        }
        
        var activity = parentId != null
            ? ActivitySource.StartActivity(name, kind, parentId)
            : ActivitySource.StartActivity(name, kind);
        
        activity?.AddTag("adapter", "laters");
        return activity;
    }
}
