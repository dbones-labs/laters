namespace Laters.Data.Marten;

using global::Marten;
using global::Marten.Schema;
using global::Marten.Schema.Identity;
using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Frames;
using JasperFx.Core.Reflection;
using Microsoft.Extensions.DependencyInjection;

public class Marten : StorageSetup
{
    protected override void Apply(IServiceCollection collection)
    {
        collection.AddScoped<ISession>();
    }
}

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
}

public class StringIdGeneration : IIdGeneration
{
    public void GenerateCode(GeneratedMethod method, DocumentMapping mapping)
    {
        Use use = new Use(mapping.DocumentType);
        method.Frames.Code(
            $"if ({{0}}.{mapping.IdMember.Name} == null)" +
            $" _setter({{0}}, {typeof(Convert).FullNameInCode()}" +
            $".ToBase64String({typeof(Guid).FullNameInCode()}.NewGuid().ToByteArray()).Replace(\"/\", \"_\").Replace(\"+\", \"-\").Substring(0, 22));",
            use);
        method.Frames.Code("return {0}." + mapping.IdMember.Name + ";", use);
    }

    public IEnumerable<Type> KeyTypes { get; } = new[] { typeof(string) };
    public bool RequiresSequences => false;
}