namespace Laters.Data.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;


/// <summary>
/// context for queries
/// </summary>
public class LatersQueryDbContext : LatersDbContextBase
{
    /// <summary>
    /// creates the context
    /// </summary>
    /// <param name="options">config options</param>
    public LatersQueryDbContext(DbContextOptions<LatersQueryDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// this context is read-only, so we throw if they try to save
    /// </summary>
    public override int SaveChanges()
    {
        // Throw if they try to call this
        throw new InvalidOperationException("This context is read-only.");
    }
}