namespace Laters.Data.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Models;


/// <summary>
/// generates string ids if the instance does not have one assigned
/// </summary>
public class StringIdGenerator : ValueGenerator
{
    /// <summary>
    /// no temporary values
    /// </summary>
    public override bool GeneratesTemporaryValues => false;
    
    /// <summary>
    /// applies the id if it is not already set
    /// </summary>
    /// <param name="entry">the entity to apply ids with</param>
    protected override object? NextValue(EntityEntry entry)
    {
        var entity = entry.Entity as Entity;
        if (entity == null)
        {
            throw new NotSupportedException($"cannot generate if for {entry.Entity.GetType().FullName}");
        }

        if (!string.IsNullOrEmpty(entity.Id))
        {
            return entity.Id;
        }

        var guid = Guid.NewGuid();
        var based = Convert.ToBase64String(guid.ToByteArray());
        var result = based
            .Replace("/","_")
            .Replace("+", "-")
            .Substring(0, 22);

        return result;
    }
}