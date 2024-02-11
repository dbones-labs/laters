namespace Laters.Configuration;

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