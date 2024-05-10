namespace Laters.Data.Marten;

using global::Marten;
using Infrastructure;
using JasperFx.Core;
using Models;
using ServerProcessing;


#pragma warning disable 1591 //xml comments

public class Session : ISession
{
    readonly IDocumentSession _documentSession;
    readonly IQuerySession _querySession;

    public Session(IDocumentSession documentSession, IQuerySession querySession)
    {
        _documentSession = documentSession;
        _querySession = querySession;
    }

    public async Task<T?> GetById<T>(string id) where T : Entity
    {
        var item = await _documentSession.LoadAsync<T>(id);
        return item;
    }

    public Task<IEnumerable<CronJob>> GetGlobalCronJobs(int skip = 0, int take = 50)
    {
        var items = _documentSession.Query<CronJob>()
            .Where(x => x.IsGlobal)
            .Skip(skip)
            .Take(take);
        return Task.FromResult<IEnumerable<CronJob>>(items);
    }

    public Task<List<Candidate>> GetJobsToProcess(List<string> rateLimitNames, int skip = 0, int take = 50)
    {
        var items = _querySession
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
            .ToList();

        return Task.FromResult(items);
    }

    public void Store<T>(T item) where T : Entity
    {
        _documentSession.Store(item);
    }

    public void Delete<T>(string id) where T : Entity
    {
        _documentSession.Delete<T>(id);
    }

    public void DeleteOrphin(string cronName)
    {
        DeleteOrphan(cronName);
    }

    public void DeleteOrphan(string cronName)
    {
        _documentSession.DeleteWhere<Job>(x => x.ParentCron == cronName);
    }

    public async Task SaveChanges()
    {
        try
        {
            await _documentSession.SaveChangesAsync();
        }
        catch (global::Marten.Exceptions.ConcurrencyException e)
        {
            throw new ConcurrencyException(e);
        }
    }

    public Task<IEnumerable<CronJob>> GetGlobalCronJobsWithOutJob(int skip = 0, int take = 50)
    {
        var cronJobs = _documentSession.Query<CronJob>()
            .Where(x => x.LastTimeJobSynced <= DateTime.MinValue.AddSeconds(1))
            .Skip(skip)
            .Take(take)
            .ToList();
            
        return Task.FromResult<IEnumerable<CronJob>>(cronJobs);
    }
}
