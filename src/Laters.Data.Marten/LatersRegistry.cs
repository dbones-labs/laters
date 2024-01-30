namespace Laters.Data.Marten;

using global::Marten.Schema.Identity;
using Models;

public class LatersRegistry : global::Marten.MartenRegistry
{
    public LatersRegistry()
    {
        For<Leader>()
            .Identity(x => x.Id)
            .UseOptimisticConcurrency(true)
            .IdStrategy(new StringIdGeneration());
        
        For<Job>()
            .UseOptimisticConcurrency(true)
            .Identity(x => x.Id)
            .IdStrategy(new StringIdGeneration());
        
        For<CronJob>()
            .UseOptimisticConcurrency(true)
            .Identity(x => x.Id)
            .IdStrategy(new StringIdGeneration());
    }
}