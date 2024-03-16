namespace Laters.Data.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;

public class LatersQueryDbContext : LatersDbContextBase
{
    public override int SaveChanges()
    {
        // Throw if they try to call this
        throw new InvalidOperationException("This context is read-only.");
    }
}