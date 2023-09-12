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
    public int InMemoryWorkerQueueMax { get; set; }

    /// <summary>
    /// the number of items to process at the same time.
    /// </summary>
    public int NumberOfProcessingThreads { get; set; }

    /// <summary>
    /// allow private certs for worker endpoints
    /// </summary>
    public bool AllowPrivateCert { get; set; }

    public List<RateWindow> Windows = new();
}