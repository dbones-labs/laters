namespace Laters.Models;

using System.ComponentModel.DataAnnotations;
using ClientProcessing.Middleware;

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

    public Job GetNextJob(ICrontab crontab)
    {
        return new Job()
        {
            ScheduledFor = crontab.Next(Cron),
            JobType = JobType,
            Payload = Payload,
            Headers = Headers,
            ParentCron = Id,
            TimeToLiveInSeconds = TimeToLiveInSeconds,
            WindowName = WindowName
        };
    }
}

