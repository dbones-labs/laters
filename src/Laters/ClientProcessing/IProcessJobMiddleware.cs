namespace Laters.ClientProcessing;


/// <summary>
/// the client pipeline for dealing with jobs
/// </summary>
/// <typeparam name="T">the job type this 1 pipeline will deal with</typeparam>
public interface IProcessJobMiddleware<T> 
{
    /// <summary>
    /// we execute this for each message
    /// </summary>
    /// <param name="scope">the current scoped ioc container</param>
    /// <param name="context">the job, which will only have the ID and Type to start with</param>
    /// <returns>Task for asyc processing</returns>
    public Task Execute(IServiceProvider scope, JobContext<T> context);
}