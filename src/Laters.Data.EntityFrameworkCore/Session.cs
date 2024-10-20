namespace Laters.Data.EntityFrameworkCore;

using System.Data;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Models;
using ServerProcessing;

/// <summary>
/// The session for the entity framework core
/// </summary>
public class Session : ISession
{
    readonly LatersDbContext _dbContext;
    readonly LatersQueryDbContext _queryDbContext;
    readonly TransactionCoordinator _transactionCoordinator;

    /// <summary>
    /// create the session
    /// </summary>
    /// <param name="dbContext">laters db context</param>
    /// <param name="queryDbContext">the query only db context</param>
    /// <param name="transactionCoordinator">the transaction coordinator which will</param>
    public Session(
        LatersDbContext dbContext, 
        LatersQueryDbContext queryDbContext, 
        TransactionCoordinator transactionCoordinator)
    {
        _dbContext = dbContext;
        _queryDbContext = queryDbContext;
        _transactionCoordinator = transactionCoordinator;
    }
    
    /// <inheritdoc/>
    public async Task<T?> GetById<T>(string id, CancellationToken cancellationToken = default) where T : Entity
    {
        var entity = await _dbContext
            .GetDbSet<T>()
            .FirstOrDefaultAsync(x=> x.Id == id, cancellationToken);

        return entity;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CronJob>> GetGlobalCronJobs(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.CronJobs
            .Where(x => x.IsGlobal)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return items;
    }

    /// <inheritdoc/>
    public async Task<List<Candidate>> GetJobsToProcess(List<string> rateLimitNames, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var items = await _queryDbContext.Jobs
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
                JobType = x.JobType,
                TraceId = x.TraceId
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void Delete<T>(string id) where T : Entity
    {
        var set = _dbContext.GetDbSet<T>();
        var entity = set.FirstOrDefault(x => x.Id == id);
        if (entity is not null)
        {
            set.Remove(entity);
        }
    }

    /// <inheritdoc/>
    public void DeleteOrphan(string cronName)
    {
        var set = _dbContext.Jobs;
        var entities = set.Where(x => x.ParentCron == cronName);
        if (entities.Any())
        {
            set.RemoveRange(entities);
        }
    }

    /// <inheritdoc/>
    public async Task SaveChanges(CancellationToken cancellationToken = default)
    {
        try
        {
            await _transactionCoordinator.Commit(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException(ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CronJob>> GetGlobalCronJobsWithOutJob(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.CronJobs
            .Where(x=> x.LastTimeJobSynced <= DateTime.MinValue)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return items;
    }
}