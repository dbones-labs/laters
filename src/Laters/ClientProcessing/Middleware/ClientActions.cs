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
    public Type LoadJobIntoContextAction { get; set; } = typeof(LoadJobIntoContextAction<>);
    
    /// <summary>
    /// onced processed if the job is part of a cronjob, then create and queue next (applied third)
    /// </summary>
    public Type QueueNextAction { get; set; } = typeof(QueueNextAction<>);
    
    /// <summary>
    /// any and all custom actions (applied 4th and onwards, in order)
    /// </summary>
    public List<Type> CustomActions { get; set; } = new();

    /// <summary>
    /// this is the handler action, which will execute the job (applied last)
    /// </summary>
    public Type MainAction { get; set; } = typeof(HandlerAction<>);
}