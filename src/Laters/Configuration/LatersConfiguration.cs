namespace Laters;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// setup
/// </summary>
public class LatersConfiguration
{ 
    public int LeaderTimeToLiveInSeconds { get; set; } = 5;
    
    /// <summary>
    /// this is the location of the lb which will point at all the workers.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string WorkerEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// this is the max size of the in memory queue, for items ready to be processed
    /// </summary>
    public int InMemoryWorkerQueueMax { get; set; } = 45;

    /// <summary>
    /// the number of items to process at the same time.
    /// </summary>
    public int NumberOfProcessingThreads { get; set; } = 10;

    /// <summary>
    /// allow private certs for worker endpoints
    /// </summary>
    public bool AllowPrivateCert { get; set; }

    public Dictionary<string, RateWindow> Windows { get; set; } = new();
    public Roles Role { get; set; } = Roles.Any;
}

/// <summary>
/// determine the responsibility the instance takes.
/// </summary>
public enum Roles
{
    /// <summary>
    /// the running instance has the possibility to be promoted to the leader
    /// and will also be a worker, this is the default
    /// </summary>
    Any,
    
    /// <summary>
    /// for a single instance, this does not need to know of domain logic (as it will)
    /// it will solely be the leader,
    /// <br />- ensure this instance has auto-recovery (i.e. runs in kubernetes)
    /// <br />- ensure that you have have a pool of <see cref="Worker"/> instances
    /// </summary>
    /// <remarks>
    /// This will not pole for leader election, it will always assume this instance is the leader
    /// </remarks>
    OnlyLeader,
    
    /// <summary>
    /// this instance will only process jobs, it will contain the business logic. 
    /// it will will not try to become a leader.
    /// </summary>
    Worker
}