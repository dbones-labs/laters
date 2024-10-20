namespace Laters.Configuration;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// setup
/// </summary>
public class LatersConfiguration
{
    /// <summary>
    /// the length of time a leader has before it checks in
    /// letting all other potential leaders its still alive
    /// </summary>
    public int LeaderTimeToLiveInSeconds { get; set; } = 5;

    /// <summary>
    /// How often to check if the leader is still alive or elect a new one
    /// </summary>
    public int CheckLeaderInSeconds {get; set;} = 3;
    
    /// <summary>
    /// this is the location of the lb which will point at all the workers.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? WorkerEndpoint { get; set; }

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

    /// <summary>
    /// this allows you to throttle particular jobs
    /// </summary>
    public Dictionary<string, RateWindow> Windows { get; set; } = new();
    
    /// <summary>
    /// this is the role of this instance.
    /// </summary>
    public Roles Role { get; set; } = Roles.All;

    
    /// <summary>
    /// how long to wait before checking the DB for jobs
    /// </summary>
    public int CheckDatabaseInSeconds { get; set; } = 3;

    /// <summary>
    /// set how often we check the telemetry from the storage
    /// </summary>
    public int CheckTelemetryInSeconds { get; set; } = 15;
}