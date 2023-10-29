namespace Laters;


/// <summary>
/// a queued job, to be processed
/// </summary>
public class Job : JobBase
{
    /// <summary>
    /// the number of attempts we have tried to process this job
    /// </summary>
    public virtual int Attempts { get; set; }
    
    /// <summary>
    /// indicates we have failed to process a job, this will now be ignored from
    /// processing, allow you to debug and decide what to do with it.
    /// </summary>
    public virtual bool DeadLettered { get; set; }
    
    /// <summary>
    /// when the job will be processed
    /// </summary>
    public virtual DateTime ScheduledFor { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// the parent cron, if this was created via a cron.
    /// </summary>
    public virtual string? ParentCron { get; set; }

    /// <summary>
    /// time of last execution
    /// </summary>
    public DateTime LastAttempted { get; set; }
}