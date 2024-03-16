namespace Laters.Data.EntityFrameworkCore;

using System.Data;
using System.Data.Common;
using Infrastucture;
using Microsoft.EntityFrameworkCore;
using Models;
using ServerProcessing;

public class Session : ISession
{
    readonly LatersDbContext _dbContext;
    readonly LatersQueryDbContext _queryDbContext;
    readonly WriteConnectionWrapper _conn;

    public Session(LatersDbContext dbContext, LatersQueryDbContext queryDbContext, WriteConnectionWrapper conn)
    {
        _dbContext = dbContext;
        _queryDbContext = queryDbContext;
        _conn = conn;
    }
    
    public async Task<T?> GetById<T>(string id) where T : Entity
    {
        var entity = await _dbContext
            .GetDbSet<T>()
            .FirstOrDefaultAsync(x=> x.Id == id);
        return entity;
    }

    public Task<IEnumerable<CronJob>> GetGlobalCronJobs()
    {
        var items = _dbContext.CronJobs.Where(x => x.IsGlobal);
        return Task.FromResult<IEnumerable<CronJob>>(items);
    }

    public Task<List<Candidate>> GetJobsToProcess(List<string> rateLimitNames, int skip = 0, int take = 50)
    {
        var items = _queryDbContext.Jobs
            .Where(x => x.ScheduledFor <= SystemDateTime.UtcNow)
            .Where(x => !x.DeadLettered)
            .Where(x => rateLimitNames.Contains(x.WindowName))
            .OrderByDescending(x => x.ScheduledFor)
            .Skip(skip)
            .Take(take)
            .Select(x => new Candidate()
            {
                WindowName = x.WindowName,
                Id = x.Id,
                JobType = x.JobType
            })
            .ToList();

        return Task.FromResult(items);
    }

    public void Store<T>(T entity) where T : Entity
    {
        if (string.IsNullOrEmpty(entity.Id) || _dbContext.GetDbSet<T>().Count(x => x.Id == entity.Id) == 0)
        {
            _dbContext.GetDbSet<T>().Add(entity);
        }
        else
        {
            //this should not be called, as the entity should already be in the unit of work.
            _dbContext.GetDbSet<T>().Update(entity);
        }
        
    }

    public void Delete<T>(string id) where T : Entity
    {
        var set = _dbContext.GetDbSet<T>();
        var entity = set.FirstOrDefault(x => x.Id == id);
        if (entity is not null)
        {
            set.Remove(entity);
        }
    }

    public void DeleteOrphin(string cronName)
    {
        var set = _dbContext.Jobs;
        var entities = set.Where(x => x.ParentCron == cronName);
        if (entities.Any())
        {
            set.RemoveRange(entities);
        }
    }

    
    /// <summary>
    /// this is only called by Laters, so we will make use of the transaction
    /// </summary>
    /// <exception cref="ConcurrencyException"></exception>
    public async Task SaveChanges()
    {
        try
        {
            using var tx = _conn.Connection.BeginTransaction(IsolationLevel.Serializable);
            _dbContext.Database.UseTransaction(tx);
            
            await _dbContext.SaveChangesAsync();
            tx.Commit();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException(ex);
        }
    }
}