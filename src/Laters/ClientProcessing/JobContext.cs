namespace Laters.ClientProcessing;

using System.ComponentModel.DataAnnotations;
using Models;

/// <summary>
/// the context of which this job is being processed
/// </summary>
/// <typeparam name="T">the parameters which have been passed into this job</typeparam>
public class JobContext<T>
{
    /// <summary>
    /// the information the job is bring processed for
    /// </summary>
    [Required]
    public T Payload { get; set; } = default!;

    /// <summary>
    /// this will allow you to know when the job have been cancelled
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = default!;

    /// <summary>
    /// the job, consider this the envelope for the Payload
    /// </summary>
    public Job Job { get; set; } = null!; 

    /// <summary>
    /// the id of the job
    /// </summary>
    public string JobId => ServerRequested?.Id ?? string.Empty;
    
    /// <summary>
    /// the information we are provided by the leader
    /// </summary>
    public ProcessJob ServerRequested{ get; set; } = null!;
}