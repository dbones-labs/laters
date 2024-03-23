namespace Laters.Data.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;
using Models;


/// <summary>
/// this is the base context for the laters database, where we define all the entities
/// for both the <see cref="LatersDbContext"/> and the <see cref="LatersQueryDbContext"/>
/// </summary>
public abstract class LatersDbContextBase : DbContext
{ 
    /// <summary>
    /// creaes the context
    /// </summary>
    /// <param name="options">the options for the context</param>
    public LatersDbContextBase(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// the <see cref="Job"/> Set
    /// </summary>
    public DbSet<Job> Jobs { get; set; } = null!;

    /// <summary>
    /// the <see cref="CronJob"/> Set
    /// </summary>
    public DbSet<CronJob> CronJobs { get; set; } = null!;

    /// <summary>
    /// the <see cref="Leader"/> Set
    /// </summary>
    public DbSet<Leader> Leaders { get; set; } = null!;

    /// <summary>
    /// we define the overridden model creation here, mainly around ids and concurrency
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //https://learn.microsoft.com/en-us/ef/core/saving/concurrency?tabs=fluent-api
        
        modelBuilder.Entity<Job>()
            .Property(x => x.Id)
            .HasValueGenerator<StringIdGenerator>()
            .ValueGeneratedOnAdd();
        
        modelBuilder.Entity<Job>()
            .Property(p => p.Revision)
            .IsConcurrencyToken();

        modelBuilder.Entity<CronJob>()
            .Property(x => x.Id)
            .HasValueGenerator<StringIdGenerator>()
            .ValueGeneratedOnAdd();
        
        modelBuilder.Entity<CronJob>()
            .Property(p => p.Revision)
            .IsConcurrencyToken();

        modelBuilder.Entity<Leader>()
            .Property(x => x.Id)
            .HasValueGenerator<StringIdGenerator>()
            .ValueGeneratedOnAdd();
        
        modelBuilder.Entity<Leader>()
            .Property(p => p.Revision)
            .IsConcurrencyToken();
    }
    
    /// <summary>
    /// gets the db set for the given type
    /// </summary>
    /// <typeparam name="T">the type we are after</typeparam>
    public DbSet<T> GetDbSet<T>() where T : Entity
    {
        if (typeof(T) == typeof(Job))
        {
            return Jobs as DbSet<T>;
        }

        if (typeof(T) == typeof(CronJob))
        {
            return CronJobs as DbSet<T>;
        }

        if (typeof(T) == typeof(Leader))
        {
            return Leaders as DbSet<T>;
        }

        throw new ArgumentException($"Type {typeof(T)} is not supported.");
    }
}