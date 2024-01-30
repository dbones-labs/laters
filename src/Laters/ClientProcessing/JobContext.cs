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
    public string JobId { get; set; }
}


/// <summary>
/// this is 
/// </summary>
public interface IStartUpFilter
{
    void Configure();
}