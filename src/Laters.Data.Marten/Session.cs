namespace Laters.Data.Marten;

using global::Marten;

public class Session : ISession, IAsyncDisposable
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

    public Task<IEnumerable<Server>> GetServers()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<CronJob>> GetGlobalCronJobs()
    {
        var items = _documentSession.Query<CronJob>().Where(x => x.IsGlobal);
        return Task.FromResult<IEnumerable<CronJob>>(items);
    }

    public Task<List<Candidate>> GetJobsToProcess(List<string> rateLimitNames, int take = 50)
    {
        var items = _querySession
            .Query<Job>()
            .Where(x => rateLimitNames.Contains(x.WindowName))
            .OrderByDescending(x => x.ScheduledFor)
            .Take(take)
            .Select(x => new Candidate()
            {
                WindowName = x.WindowName,
                Id = x.Id,
                JobType = x.JobType
            });

        return Task.FromResult<List<Candidate>>(items.ToList());
    }

    public void Store<T>(T item) where T : Entity
    {
        _documentSession.Store(item);
    }

    public void Delete<T>(string id) where T : Entity
    {
        _documentSession.Delete(id);
    }

    public void DeleteOrphin(string cronName)
    {
        _documentSession.DeleteWhere<Job>(x => x.ParentCron == cronName);
    }

    public async Task SaveChanges()
    {
        await _documentSession.SaveChangesAsync();
    }

    public void Dispose()
    {
        _documentSession.Dispose();
        _querySession.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _documentSession.DisposeAsync();
        await _querySession.DisposeAsync();
    }
}