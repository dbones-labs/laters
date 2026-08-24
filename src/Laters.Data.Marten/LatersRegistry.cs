namespace Laters.Data.Marten;

using Models;

public class LatersRegistry : global::Marten.MartenRegistry
{
    public LatersRegistry()
    {
        For<Leader>()
            .Identity(x => x.Id)
            .UseOptimisticConcurrency(true)
            .Identification(member => new StringIdentification<Leader>(member));
        
        For<Job>()
            .UseOptimisticConcurrency(true)
            .Identity(x => x.Id)
            .Identification(member => new StringIdentification<Job>(member));
        
        For<CronJob>()
            .UseOptimisticConcurrency(true)
            .Identity(x => x.Id)
            .Identification(member => new StringIdentification<CronJob>(member));
    }
}
