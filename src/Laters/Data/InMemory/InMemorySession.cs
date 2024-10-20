namespace Laters.Data.InMemory;

using System.Text.Json;
using Data;
using Infrastructure;
using Models;
using ServerProcessing;

/// <summary>
/// this is not for production use
/// </summary>
public class InMemorySession : ISession
{
    readonly InMemoryStore _store;
    readonly HashSet<string> _removeEntities = new();
    
    /// <summary>
    /// create an instance of the session
    /// </summary>
    /// <param name="store">the memory backing store</param>
    public InMemorySession(InMemoryStore store)
    {
        _store = store;
    }

    /// <summary>
    /// this is the store for the unit of work pattern to store changes with.
    /// </summary>
    public Dictionary<string, Entity> UnitOfWork { get; init; } = new();

    /// <inheritdoc/>
    public Task<T?> GetById<T>(string id, CancellationToken cancellationToken = default) where T : Entity
    {
        if (UnitOfWork.TryGetValue(id, out var entry)) return Task.FromResult(entry as T);
        if (_store.Data.TryGetValue(id, out var storedEntry))
        {
            var copy = DeepCopy(storedEntry);
            UnitOfWork.Add(storedEntry.Id, copy);
            return Task.FromResult(copy as T);
        }
        
        T? none = null;
        return Task.FromResult(none);
    }

    /// <inheritdoc/>
    IEnumerable<T> GetEntities<T>() where T: Entity
    {
        var scoped = UnitOfWork.Where(x => x is T).ToDictionary(x=> x.Key, x=> x.Value);
        var stored = _store.Data.Where(x => !scoped.ContainsKey(x.Key)).Where(x => x is T);
        
        var results = scoped.Select(x => x.Value).Union(stored.Select(y => y.Value)).Cast<T>();
        return results;
    }
    
    /// <inheritdoc/>
    public static T DeepCopy<T>(T other)
    {
        var payload = JsonSerializer.Serialize(other);
        var result = JsonSerializer.Deserialize<T>(payload);
        if (result is null)
        {
            throw new Exception($"cannot DeepCopy {typeof(T)}");
        }
        return result;
    }
    
    /// <inheritdoc/>
    public Task<List<Candidate>> GetJobsToProcess(List<string> rateLimitNames, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var items = GetEntities<Job>()
            .Where(x => x.ScheduledFor <= SystemDateTime.UtcNow)
            .Where(x => !x.DeadLettered)
            .Where(x => rateLimitNames.Contains(x.WindowName))
            .OrderByDescending(x => x.ScheduledFor)
            .Skip(skip)
            .Take(take)
            .Select(x =>
            {
                //load into UoW (and if present, return the current)
                if(UnitOfWork.TryGetValue(x.Id, out var scoped)) return scoped as Job;
                UnitOfWork.Add(x.Id, DeepCopy(x));
                return x;
            })
            .Where(x => x is not null)
            .Select(x => new Candidate()
            {
                WindowName = x!.WindowName,
                Id = x.Id,
                JobType = x.JobType,
                TraceId = x.TraceId 
            })
            .ToList();

        return Task.FromResult(items);
    }

    /// <inheritdoc/>
    public void Store<T>(T entity) where T : Entity
    {
        if(UnitOfWork.ContainsKey(entity.Id)) return;
        UnitOfWork.Add(entity.Id, entity);
    }

    /// <inheritdoc/>
    public void Delete<T>(string id) where T : Entity
    {
        UnitOfWork.Remove(id);
        _removeEntities.Remove(id);
    }

    /// <inheritdoc/>
    public void DeleteOrphan(string cronName)
    {
        var job = GetEntities<Job>().FirstOrDefault(x => x.ParentCron == cronName);
        if (job is not null)
        {
            Delete<Job>(job.Id);
        }
    }

    /// <inheritdoc/>
    public Task SaveChanges(CancellationToken cancellationToken = default)
    {
        _store.Commit(entities =>
        {
            //check any guids first
            foreach (var entity in UnitOfWork.Values)
            {
                if (!_store.Data.TryGetValue(entity.Id, out var stored)) continue;
                if (stored.Revision == entity.Revision) continue;

                throw new ConcurrencyException(new Exception($"{entity.GetType()} - ID: {entity.Id}"));
            }
            
            //apply the removals first, then we update
            foreach (var removeId in _removeEntities)
            {
                _store.Data.Remove(removeId, out var _);
            }

            foreach (var value in UnitOfWork.Values)
            {
                value.Revision = Guid.NewGuid();
                _store.Data.AddOrUpdate(value.Id, value,  (id, entity) => value);
            }
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<CronJob>> GetGlobalCronJobs(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var results = GetEntities<CronJob>()
            .Skip(skip)
            .Take(take);

        return Task.FromResult(results);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<CronJob>> GetGlobalCronJobsWithOutJob(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var results = GetEntities<CronJob>()
            .Where(x => x.LastTimeJobSynced == DateTime.MinValue.ToUniversalTime())
            .Skip(skip)
            .Take(take);

        return Task.FromResult(results);
    }
}