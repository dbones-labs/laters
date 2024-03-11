namespace Laters.Infrastucture;

/// <summary>
/// if we are gonna do separate files, we will use this attribute to link the class which
/// class these logs belong to, and which line block is assigned to this
/// </summary>
/// <remarks>
/// experimental
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
internal class LoggerForAttribute : Attribute
{
    
    /// <summary>
    /// the thing the logs belong too
    /// </summary>
    public Type Type { get; set; }

    /// <summary>
    /// the entry in the registry
    /// </summary>
    public EventId Registry { get; set; }
}