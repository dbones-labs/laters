namespace Laters;

using System.ComponentModel.DataAnnotations;

public abstract class JobBase : Entity
{
    public virtual string? WindowName { get; set; }
    public virtual int MaxRetries { get; set; } = 5;
    public virtual int? TimeToLiveInSeconds { get; set; } = 300;
    
    [Required]
    public virtual string? Payload { get; set; }

    [Required]
    public virtual Type? JobType { get; set; }
    
    public virtual Dictionary<string, string> Headers { get; set; } = new();
}