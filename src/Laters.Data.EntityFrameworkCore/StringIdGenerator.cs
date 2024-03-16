namespace Laters.Data.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Models;

// ReSharper disable once ClassNeverInstantiated.Global
public class StringIdGenerator : ValueGenerator
{
    public override bool GeneratesTemporaryValues => false;
    
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