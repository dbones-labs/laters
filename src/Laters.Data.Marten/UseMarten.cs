namespace Laters.Data.Marten;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// sets up Marten to work with Laters
/// <br />requires the following
/// <br />- DirtyTracking
/// <br />- to add <see cref="LatersRegistry"/>
/// </summary>
/// <remarks>
/// Please consider adding support for UTC datetime 
/// <code>AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);</code>
/// </remarks>
public class UseMarten : StorageSetup
{
    protected override void Apply(IServiceCollection collection)
    {
        collection.TryAddScoped<ISession, Session>();
        collection.TryAddScoped<ITelemetrySession, TelemetrySession>();
    }
 }