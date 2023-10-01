namespace Laters;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// everything is an entity :)
/// </summary>
public class Entity
{
    /// <summary>
    /// the unique identifier
    /// </summary>
    [Required(AllowEmptyStrings = false)] 
    public virtual string Id { get; set; } = string.Empty;
    

    public virtual Guid? Revision { get; set; }
}