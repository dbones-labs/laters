namespace Laters;

/// <summary>
/// this is the payload we pass to the workers so they know what to process 
/// </summary>
public class ProcessJob
{
    public string Id { get; set; }
    public string JobType { get; set; }
}