namespace Laters.Data.InMemory;
using Data;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ISession = ISession;

/// <summary>
/// this is not for production use
/// </summary>
public class UseMemory : StorageSetup
{
    protected internal override void Apply(IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddSingleton<InMemoryStore>();
        serviceCollection.TryAddScoped<ISession, InMemorySession>();
        serviceCollection.TryAddScoped<ITelemetrySession, TelemetrySession>();
    }
}