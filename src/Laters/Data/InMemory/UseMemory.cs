namespace Laters.Data.InMemory;

using Data;
using ISession = ISession;

/// <summary>
/// this is not for production use
/// </summary>
public class UseMemory : StorageSetup
{
    protected internal override void Apply(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<InMemoryStore>();
        serviceCollection.AddScoped<ISession, InMemorySession>();
    }
}