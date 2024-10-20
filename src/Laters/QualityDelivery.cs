namespace Laters;

public class QualityDelivery
{
    /// <summary>
    /// the total times a job instance will be retried before it is dead-lettered
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// the maximum time we have in order to have completed the job
    /// (do not confuse with a timeout)
    /// </summary>
    public int? TimeToLiveInSeconds { get; set; } = 300;
    
    /// <summary>
    /// will default to the global rate limit.
    /// </summary>
    public string? WindowName { get; set; }
}