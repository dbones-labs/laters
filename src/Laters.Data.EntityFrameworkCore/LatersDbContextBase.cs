namespace Laters.Data.EntityFrameworkCore;

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Models;

public abstract class LatersDbContextBase : DbContext
{ 
    public DbSet<Job> Jobs { get; set; }
    public DbSet<CronJob> CronJobs { get; set; }
    public DbSet<Leader> Leaders { get; set; }

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