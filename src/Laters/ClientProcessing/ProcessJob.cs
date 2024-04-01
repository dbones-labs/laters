namespace Laters.ClientProcessing;

/// <summary>
/// this is the payload we pass to the workers so they know what to process 
/// </summary>
public class ProcessJob
{ 
    /// <summary>
    /// creates a new instance of <see cref="ProcessJob"/>
    /// </summary>
    /// <param name="id">job id</param>
    /// <param name="jobType">job type</param>
    /// <param name="window">name of the window</param>
    /// <param name="leaderId">leader id</param>
    public ProcessJob(string id, string jobType, string window, string leaderId)
    {
        Id = id;
        JobType = jobType;
        Window = window;
        LeaderId = leaderId;
    }

    /// <summary>
    /// used for serialization
    /// </summary>
    public ProcessJob() { }
    
    /// <summary>
    /// the id of the job to process
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// the type of the job being processed
    /// </summary>
    public string JobType { get; set; }

    /// <summary>
    /// The server ID which sent the job to be processed
    /// </summary>
    public string LeaderId { get; set; }

    /// <summary>
    /// Name of the window we are processing the job in
    /// </summary>
    public string Window { get; set; }
}