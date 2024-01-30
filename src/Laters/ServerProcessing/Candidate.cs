namespace Laters.ServerProcessing;

/// <summary>
/// this represents the candidate job we want the workers to process
/// </summary>
public class Candidate 
{
    /// <summary>
    /// the job id
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// the type of the job
    /// </summary>
    public string JobType { get; set; }
    
    /// <summary>
    /// the window name the job is going to be processed under
    /// </summary>
    public string WindowName { get; set; }
}