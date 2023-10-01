namespace Laters;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// this is a cron job, which will create a job when the cron is met. 
/// <br />- the ID has to be set by the user, and should be meaningful.
/// </summary>
public class CronJob : JobBase
{
    /// <summary>
    /// when to create a job instance.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public virtual string Cron { get; set; } = string.Empty;
    
    /// <summary>
    /// this was setup via the global mechanism, and will be managed via this.
    /// </summary>
    public virtual bool IsGlobal { get; set; }
}

[Obsolete("do not think this is in use")]
public class CronJobCtx<T>
{
    public T? Payload { get; set; }
    public CronJob? CronJob { get; set; }
}

