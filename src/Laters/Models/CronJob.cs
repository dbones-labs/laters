namespace Laters;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// name is the id
/// </summary>
public class CronJob : JobBase
{
    [Required(AllowEmptyStrings = false)]
    public virtual string Cron { get; set; } = string.Empty;
    
    /// <summary>
    /// this was setup via the global mechanism, and will be managed via this.
    /// </summary>
    public virtual bool IsGlobal { get; set; }
}

public class CronJobCtx<T>
{
    public T? Payload { get; set; }
    public CronJob? CronJob { get; set; }
}

