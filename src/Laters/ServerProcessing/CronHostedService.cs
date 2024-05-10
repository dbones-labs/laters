namespace Laters.ServerProcessing;

using Laters.Configuration;
using Laters.ServerProcessing.Triggers;
using Laters.Models;
using Laters.Infrastructure;

/// <summary>
/// this is to ensure that the CronJobs have and instance of a job.
/// </summary>
public class EnsureJobInstancesForCron
{
    readonly LeaderContext _leaderContext;
    readonly IServiceProvider _serviceProvider;
    readonly LatersConfiguration _configuration;
    readonly ILogger<EnsureJobInstancesForCron> _logger;
    readonly ContinuousLambda _populateLambda;

    /// <summary>
    /// create a new instance of the EnsureJobInstancesForCron
    /// </summary>
    /// <param name="leaderContext"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="configuration"></param>
    /// <param name="logger"></param>
    public EnsureJobInstancesForCron(
        LeaderContext leaderContext,
        IServiceProvider serviceProvider,
        LatersConfiguration configuration,
        ILogger<EnsureJobInstancesForCron> logger)
    {
        _leaderContext = leaderContext;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;

        var trigger = new TimeTrigger(TimeSpan.FromSeconds(_configuration.CheckDatabaseInSeconds));

        _populateLambda =
            new ContinuousLambda(nameof(EnsureJobInstances), async () => await EnsureJobInstances(), trigger);
    }


    /// <summary>
    /// start the EnsureJobInstancesForCron
    /// </summary>
    /// <param name="cancellationToken"></param>
    public void Initialize(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initialize the CronJob Sync component");
        _populateLambda.Start(cancellationToken);
    }

    /// <summary>
    /// ensure that CronJobs have an instance of a job.
    /// </summary>
    /// <param name="cancellationToken">dis</param>
    protected virtual async Task EnsureJobInstances(CancellationToken cancellationToken = default)
    {
        if (!_leaderContext.IsLeader) return;

        List<CronJob> cronJobs = new();
        do
        {

            using var scope = _serviceProvider.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<Data.ISession>();
            var schedule = scope.ServiceProvider.GetRequiredService<IAdvancedSchedule>();
            cronJobs = (await session.GetGlobalCronJobsWithOutJob(0, 50)).ToList();
            
            foreach (var cronJob in cronJobs)
            {
                schedule.ForLaterNext(cronJob);
                cronJob.LastTimeJobSynced = SystemDateTime.UtcNow;
            }

            await session.SaveChanges();

        } while (cronJobs.Count != 0);

    }
}