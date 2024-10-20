namespace Laters.Data;

public abstract class StorageSetup
{
    protected internal abstract void Apply(IServiceCollection serviceCollection);
}