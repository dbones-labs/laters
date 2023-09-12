namespace Laters;

public class Job : JobBase
{
    public virtual int Attempts { get; set; }
    public virtual bool DeadLettered { get; set; }
    
    public virtual DateTime ScheduledFor { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// the parent cron, if this was created via a cron.
    /// </summary>
    public virtual string? ParentCron { get; set; }
}