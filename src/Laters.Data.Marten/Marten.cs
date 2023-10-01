namespace Laters.Data.Marten;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// sets up Marten to work with Laters
/// <br />requires the following
/// <br />- DirtyTracking
/// <br />- to add <see cref="LatersRegistry"/>
/// </summary>
public class Marten : StorageSetup
{
    protected override void Apply(IServiceCollection collection)
    {
        collection.AddScoped<ISession, Session>();
    }
}