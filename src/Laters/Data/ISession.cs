namespace Laters.Data;

using Models;
using ServerProcessing;

/// <summary>
/// datastore interactions
/// </summary>
public interface ISession : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// the an entity by its ID
    /// </summary>
    /// <param name="id">the id of the entity</param>
    /// <typeparam name="T">the type</typeparam>
    /// <returns>the instance of the entity</returns>
    public Task<T?> GetById<T>(string id) where T : Entity;

    /// <summary>
    /// get the Cron Jobs which are setup globally
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<CronJob>> GetGlobalCronJobs();

    /// <summary>
    /// gets next set the jobs to be processed
    /// </summary>
    /// <param name="ids">ids of job ids which are being processed</param>
    /// <param name="rateLimitNames">the window names which are open to process, to filter for</param>
    /// <param name="skip">the number rows to skip</param>
    /// <param name="take">the number of jobs to load in</param>
    /// <returns>all the jobs which meet our criteria, and which have not been dead lettered</returns>
    public Task<List<Candidate>> GetJobsToProcess(List<string> ids, List<string> rateLimitNames, int skip = 0, int take = 50);
    

    /// <summary>
    /// adds an item to be stored
    /// </summary>
    /// <param name="entity">the item to add</param>
    /// <typeparam name="T">the type of the item</typeparam>
    public void Store<T>(T entity) where T: Entity;

    /// <summary>
    /// deletes an <see cref="Entity"/>
    /// </summary>
    /// <param name="id">id of the entity to remove</param>
    /// <typeparam name="T">the type to remove</typeparam>
    public void Delete<T>(string id) where T : Entity;

    /// <summary>
    /// this will delete any jobs which belong to a cronJob
    /// </summary>
    /// <param name="cronName">name of the cronJob</param>
    public void DeleteOrphin(string cronName);
    
    /// <summary>
    /// unit of work pattern, this will comment any dirty objects
    /// </summary>
    /// <returns>async</returns>
    public Task SaveChanges();
    
}