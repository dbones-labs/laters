namespace Laters.Data;

/// <summary>
/// a counter entry
/// </summary>
public struct JobCounter 
{
    /// <summary>
    /// the job type
    /// </summary>
    public string JobType { get; set; }

    /// <summary>
    /// number of occurrences
    /// </summary>
    public long Count { get; set; }

}
