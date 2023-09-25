namespace Laters.Data.Marten;

using Microsoft.Extensions.DependencyInjection;

public class Marten : StorageSetup
{
    protected override void Apply(IServiceCollection collection)
    {
        collection.AddScoped<ISession>();
    }
}