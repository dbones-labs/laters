namespace Laters.Data.Marten;

public class LatersRegistry : global::Marten.MartenRegistry
{
    public LatersRegistry()
    {
        For<LeaderServer>()
            .UseOptimisticConcurrency(true)
            .IdStrategy(new StringIdGeneration());
        
        For<JobBase>()
            .AddSubClass<Job>()
            .AddSubClass<CronJob>()
            .UseOptimisticConcurrency(true)
            .IdStrategy(new StringIdGeneration());
    }
}