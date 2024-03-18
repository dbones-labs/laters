using Microsoft.EntityFrameworkCore;

namespace Laters.Data.EntityFrameworkCore;

public class LatersDbContext : LatersDbContextBase
{

    public LatersDbContext(DbContextOptions<LatersDbContext> options) : base(options)
    {
    }
}