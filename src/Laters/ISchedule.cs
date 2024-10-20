namespace Laters;

/// <summary>
/// schedule a job for later
/// </summary>
public interface ISchedule : IScheduleCron
{ 
    /// <summary>
    /// run once
    /// </summary>
    /// <param name="jobPayload">the data to run the job with</param>
    /// <param name="options">any extra parameters, if not supplied the task will be queued for immediate execution</param>
    /// <typeparam name="T">indicate the job being scheduled</typeparam>
    string ForLater<T>(T jobPayload, OnceOptions? options = null);

    /// <summary>
    /// run once
    /// </summary>
    /// <param name="jobPayload">the data to run the job with</param>
    /// <param name="scheduleFor">when to try and execute the job</param>
    /// <param name="options">any extra parameters, if not supplied the task will be queued for immediate execution</param>
    /// <typeparam name="T">indicate the job being scheduled</typeparam>
    string ForLater<T>(T jobPayload, DateTime scheduleFor, OnceOptions? options = null);
    
    /// <summary>
    /// remove a single scheduled job
    /// </summary>
    /// <param name="id">the id of the job to remove</param>
    void ForgetAboutIt<T>(string id);
    
}