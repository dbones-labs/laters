namespace Laters;

using System.ComponentModel.DataAnnotations;

public abstract class JobBase : Entity
{
    [Required]
    public virtual string? WindowName { get; set; }
    public virtual int MaxRetries { get; set; } = 5;
    public virtual int? TimeToLiveInSeconds { get; set; } = 300;
    
    [Required]
    public virtual string? Payload { get; set; }

    [Required]
    public virtual string? JobType { get; set; }
    
    public virtual Dictionary<string, string> Headers { get; set; } = new();
}