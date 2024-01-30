namespace Laters.Models;

/// <summary>
/// this denotes a SINGLE entry in the data, which 
/// denotes which running instance is the Leader.
/// </summary>
public class Leader : Entity
{
    /// <summary>
    /// the running instance id (which is a simple random number) 
    /// which is assigned at application start
    /// </summary>
    public string ServerId { get; set; }

    /// <summary>
    /// the last time the leader check-in 
    /// this is used to see if a leader died without 
    /// unexpectedly
    /// </summary>
    public DateTime Updated { get; set; }
}