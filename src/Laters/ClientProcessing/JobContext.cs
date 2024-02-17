namespace Laters.ClientProcessing;

using System.ComponentModel.DataAnnotations;
using Models;

public class JobContext<T>
{
    /// <summary>
    /// the information the job is bring processed for
    /// </summary>
    [Required]
    public T? Payload { get; set; }

    /// <summary>
    /// the job, consider this the envelope for the Payload
    /// </summary>
    public Job? Job { get; set; }

    /// <summary>
    /// the id of the job
    /// </summary>
    public string JobId => ServerRequested?.Id ?? "";
    
    /// <summary>
    /// the information we are provided by the leader
    /// </summary>
    public ProcessJob? ServerRequested{ get; set; }
}