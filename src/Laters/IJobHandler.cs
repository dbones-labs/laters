namespace Laters;

/// <summary>
/// the job handler
/// </summary>
/// <typeparam name="T">the type which is being handled</typeparam>
public interface IJobHandler<T>
{
    /// <summary>
    /// the logic of the delayed task
    /// </summary>
    /// <param name="jobContext">the context the handler is running under</param>
    /// <returns>task complete</returns>
    Task Execute(JobContext<T> jobContext);
}