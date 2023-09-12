namespace Laters;

using System.ComponentModel.DataAnnotations;

public class Entity
{
    [Required(AllowEmptyStrings = false)] 
    public virtual string Id { get; set; } = string.Empty;
    public virtual Guid? Revision { get; set; }
}