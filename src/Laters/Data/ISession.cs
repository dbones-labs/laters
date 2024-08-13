namespace Laters.Data;

using Models;
using ServerProcessing;

/// <summary>
/// datastore interactions
/// </summary>
public interface ISession
{
    /// <summary>
    /// the an entity by its ID
    /// </summary>
    /// <typeparam name="T">the type</typeparam>
    /// <param name="id">the id of the entity</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>the instance of the entity</returns>
    public Task<T?> GetById<T>(string id, CancellationToken cancellationToken = default) where T : Entity;

    /// <summary>
    /// get the Cron Jobs which are setup globally
    /// </summary>
    /// <param name="skip">the number of rows to skip</param>
    /// <param name="take">the number of rows to take</param>
    /// <param name="cancellationToken">cancellation token</param>
    public Task<IEnumerable<CronJob>> GetGlobalCronJobs(int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// get the jobs assigned to a cron
    /// </summary>
    /// <param name="skip">the number of rows to skip</param>
    /// <param name="take">the number of rows to take</param>
    /// <param name="cancellationToken">cancellation token</param>
    public Task<IEnumerable<CronJob>> GetGlobalCronJobsWithOutJob(int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// gets next set the jobs to be processed
    /// </summary>
    /// <param name="rateLimitNames">the window names which are open to process, to filter for</param>
    /// <param name="skip">the number rows to skip</param>
    /// <param name="take">the number of jobs to load in</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>all the jobs which meet our criteria, and which have not been dead lettered</returns>
    public Task<List<Candidate>> GetJobsToProcess(List<string> rateLimitNames, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    

    /// <summary>
    /// adds an item to be stored
    /// </summary>
    /// <typeparam name="T">the type of the item</typeparam>
    /// <param name="entity">the item to add</param>
    public void Store<T>(T entity) where T: Entity;

    /// <summary>
    /// deletes an <see cref="Entity"/>
    /// </summary>
    /// <typeparam name="T">the type to remove</typeparam>
    /// <param name="id">id of the entity to remove</param> 
    public void Delete<T>(string id) where T : Entity;


    /// <summary>
    /// this will delete any jobs which belong to a cronJob
    /// </summary>
    /// <param name="cronName">name of the cronJob</param>
    public void DeleteOrphan(string cronName);

    /// <summary>
    /// unit of work pattern, this will comment any dirty objects, should only be called by Laters
    /// </summary>
    /// <param name="cancellationToken">cancellation token</param>
    /// <exception cref="ConcurrencyException"></exception>
    /// <returns>async</returns>
    public Task SaveChanges(CancellationToken cancellationToken = default);
    
}