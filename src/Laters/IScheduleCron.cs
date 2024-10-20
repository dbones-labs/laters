namespace Laters;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// configure cron jobs
/// </summary>
public interface IScheduleCron
{
    /// <summary>
    /// set a job which will be executed on a Cron
    /// </summary>
    /// <param name="name">a unique name for the cron</param>
    /// <param name="jobPayload">the data for the job</param>
    /// <param name="cron">the cron on when to schedule the job instances</param>
    /// <param name="options">cron options</param>
    /// <typeparam name="T">the job to execute</typeparam>
    void ManyForLater<T>(
        [Required(AllowEmptyStrings = false)] string name, 
        T jobPayload,
        [Required(AllowEmptyStrings = false)] string cron, 
        CronOptions? options = null);

    
    /// <summary>
    /// remove a cron
    /// </summary>
    /// <param name="name">the name of the cron</param>
    /// <param name="removeOrphins">remove unprocessed jobs which are associated with this cron</param>
    /// <typeparam name="T">the job type</typeparam>
    void ForgetAboutAllOfIt<T>(
        [Required(AllowEmptyStrings = false)] string name, bool removeOrphins = true);
}