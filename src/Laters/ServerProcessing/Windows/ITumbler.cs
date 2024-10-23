namespace Laters.ServerProcessing.Windows;

/// <summary>
/// the tumbler which will coordinate all the windows and allow for rate limiting.
/// </summary>
public interface ITumbler
{
    /// <summary>
    /// start the tumbler (which will set the windows to start processing)
    /// </summary>
    /// <param name="cancellationToken"></param>
    void Initialize(CancellationToken cancellationToken);
    
    /// <summary>
    /// if we are ok to process this window
    /// </summary>
    /// <param name="windowName">the name of the window to check</param>
    /// <returns>true if we have not reached its max</returns>
    bool AreWeOkToProcessThisWindow(string windowName);
    
    /// <summary>
    /// find all windows which have available capacity
    /// </summary>
    /// <returns>all window names which can process</returns>
    List<string> GetWindowsWhichAreWithinLimits();
    
    
    /// <summary>
    /// record a job queue for a given window
    /// </summary>
    /// <param name="rateName">the window name</param>
    void RecordJobQueue(string rateName);
}