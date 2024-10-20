using Microsoft.EntityFrameworkCore;

namespace Laters.Data.EntityFrameworkCore;


/// <summary>
/// this context is used for write operations, and will be applied inside a transaction
/// </summary>
public class LatersDbContext : LatersDbContextBase
{

    /// <summary>
    ///  creates the context
    /// </summary>
    /// <param name="options">overriding options</param>
    public LatersDbContext(DbContextOptions<LatersDbContext> options) : base(options)
    {
    }
}