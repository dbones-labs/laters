namespace Laters;

public class QualityDelivery
{
    public int MaxRetries { get; set; } = 3;
    
    public int? TimeToLiveInSeconds { get; set; } = 300;
    
    /// <summary>
    /// will default to the global rate limit.
    /// </summary>
    public string? WindowName { get; set; }
}