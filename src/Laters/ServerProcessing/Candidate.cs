namespace Laters.ServerProcessing;

/// <summary>
/// this represents the candidate job we want the workers to process
/// </summary>
public class Candidate 
{
    /// <summary>
    /// the job id
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// the type of the job
    /// </summary>
    public string JobType { get; set; } = string.Empty;
    
    /// <summary>
    /// the window name the job is going to be processed under
    /// </summary>
    public string WindowName { get; set; } = string.Empty;

    /// <summary>
    /// the trace id for the this job
    /// </summary>
    public string? TraceId { get; set; }
}