namespace Laters.Data.Marten;

using global::Marten;
using Infrastructure;
using Models;
using ServerProcessing;

/// <summary>
/// The session for the Marten
/// </summary>
public class Session : ISession
{
    readonly IDocumentSession _documentSession;
    readonly IQuerySession _querySession;

    
    /// <summary>
    /// create an instance of the session
    /// </summary>
    /// <param name="documentSession">the write session from marten</param>
    /// <param name="querySession">the query session from marten</param>
    public Session(IDocumentSession documentSession, IQuerySession querySession)
    {
        _documentSession = documentSession;
        _querySession = querySession;
    }

    /// <inheritdoc/>
    public async Task<T?> GetById<T>(string id, CancellationToken cancellationToken = default) where T : Entity
    {
        var item = await _documentSession.LoadAsync<T>(id, cancellationToken);
        return item;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CronJob>> GetGlobalCronJobs(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var items = await _documentSession.Query<CronJob>()
            .Where(x => x.IsGlobal)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return items;
    }

    /// <inheritdoc/>
    public async Task<List<Candidate>> GetJobsToProcess(List<string> rateLimitNames, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var items = await _querySession
            .Query<Job>()
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

        return items.ToList();
    }

    /// <inheritdoc/>
    public void Store<T>(T item) where T : Entity
    {
        _documentSession.Store(item);
    }

    /// <inheritdoc/>
    public void Delete<T>(string id) where T : Entity
    {
        _documentSession.Delete<T>(id);
    }

    /// <inheritdoc/>
    public void DeleteOrphan(string cronName)
    {
        _documentSession.DeleteWhere<Job>(x => x.ParentCron == cronName);
    }

    /// <inheritdoc/>
    public async Task SaveChanges(CancellationToken cancellationToken = default)
    {
        try
        {
            await _documentSession.SaveChangesAsync(cancellationToken);
        }
        catch (global::Marten.Exceptions.ConcurrencyException e)
        {
            throw new ConcurrencyException(e);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CronJob>> GetGlobalCronJobsWithOutJob(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var cronJobs = await _documentSession.Query<CronJob>()
            .Where(x => x.LastTimeJobSynced <= DateTime.MinValue.AddSeconds(1))
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
            
        return cronJobs;
    }
}
