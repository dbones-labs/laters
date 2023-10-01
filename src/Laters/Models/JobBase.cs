namespace Laters;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// all jobs have a common base, detailing
/// common properties required to action a job
/// </summary>
public abstract class JobBase : Entity
{
    /// <summary>
    /// the name of the window to apply
    /// </summary>
    [Required]
    public virtual string? WindowName { get; set; } = "global";
 
    /// <summary>
    /// the number of times we should retry a job
    /// </summary>
    public virtual int MaxRetries { get; set; } = 5;
    
    /// <summary>
    /// the time in which we need to process the job within 
    /// be careful as it will DELETE the job once this time has expired
    /// </summary>
    public virtual int? TimeToLiveInSeconds { get; set; } = 10000;
    
    /// <summary>
    /// the job payload which the <see cref="IJobHandler{T}"/> will be provided to process with.
    /// </summary>
    [Required]
    public virtual string? Payload { get; set; }

    /// <summary>
    /// the job type, this is used to let us know which job handler to process
    /// </summary>
    [Required]
    public virtual string? JobType { get; set; }
    
    /// <summary>
    /// meta data to add to the processing of the job
    /// </summary>
    public virtual Dictionary<string, string> Headers { get; set; } = new();
}