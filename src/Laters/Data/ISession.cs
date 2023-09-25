namespace Laters;

/// <summary>
/// datastore interactions
/// </summary>
public interface ISession
{
    /// <summary>
    /// the an entity by its ID
    /// </summary>
    /// <param name="id">the id of the entity</param>
    /// <typeparam name="T">the type</typeparam>
    /// <returns>the instance of the entity</returns>
    public Task<T?> GetById<T>(string id) where T : Entity;
    
    /// <summary>
    /// get the servers which are running
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<Server>> GetServers();

    /// <summary>
    /// get the Cron Jobs which are setup globally
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<CronJob>> GetGlobalCronJobs();

    public Task<List<Candidate>> GetJobsToProcess(List<string> rateLimitNames, int take = 50);
    

    public void Store<T>(T item) where T: Entity;

    public void Delete<T>(string id) where T : Entity;

    public void DeleteOrphin(string cronName);
    
    public Task SaveChanges();
    
}