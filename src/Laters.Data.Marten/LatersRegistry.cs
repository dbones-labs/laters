namespace Laters.Data.Marten;

public class LatersRegistry : global::Marten.MartenRegistry
{
    public LatersRegistry()
    {
        For<Leader>()
            .UseOptimisticConcurrency(true)
            .IdStrategy(new StringIdGeneration());
        
        For<Job>()
            .UseOptimisticConcurrency(true)
            .IdStrategy(new StringIdGeneration());
        
        For<CronJob>()
            .UseOptimisticConcurrency(true)
            .IdStrategy(new StringIdGeneration());
    }
}