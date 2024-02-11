namespace Laters.ClientProcessing;

/// <summary>
/// this is the payload we pass to the workers so they know what to process 
/// </summary>
public class ProcessJob
{ 
    public ProcessJob(string id, string jobType, string leaderId)
    {
        Id = id;
        JobType = jobType;
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
}