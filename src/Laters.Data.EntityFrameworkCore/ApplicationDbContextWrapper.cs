namespace Laters.Data.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


/// <summary>
/// this will wrapper around the application's <see cref="DbContext"/>
/// </summary>
public class ApplicationDbContextWrapper 
{
    /// <summary>
    /// create an instance of the wrapper
    /// </summary>
    /// <param name="dbContext">the applications <see cref="DbContext"/></param>
    public ApplicationDbContextWrapper(DbContext dbContext)
    {
        DbContext = dbContext;
    }

    /// <summary>
    /// the applications <see cref="DbContext"/>
    /// </summary>
    public DbContext DbContext { get; }
}
