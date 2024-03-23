namespace Laters.ClientProcessing.Middleware;

/// <summary>
/// these actions are the Job Handling pipeline (remember to register with the IoC container)
/// </summary>
public class ClientActions
{
    /// <summary>
    /// handle the dead-letter and backoff (applied first)
    /// </summary>
    public Type FailureAction { get; set; } = typeof(FailureAction<>);

    /// <summary>
    /// pulls the data into memory for the rest of the pipeline to make use of (applied second)
    /// </summary>
    public Type PersistenceAction { get; set; } = typeof(PersistenceAction<>);
    
    /// <summary>
    /// when processed, if the job is part of a cronjob, then create and queue next (applied third)
    /// </summary>
    public Type QueueNextAction { get; set; } = typeof(CronAction<>);
    
    /// <summary>
    /// any and all custom actions (applied 4th and onwards, in order)
    /// </summary>
    public List<Type> CustomActions { get; set; } = new();


    #region Obsolete

    /// <summary>
    /// pulls the data into memory for the rest of the pipeline to make use of (applied second)
    /// </summary>
    [Obsolete("use PersistenceAction instead")]
    public Type LoadJobIntoContextAction { get {return PersistenceAction;} set {PersistenceAction = value;} }

    #endregion


    

}