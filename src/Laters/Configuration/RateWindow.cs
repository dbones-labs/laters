namespace Laters.Configuration;

public class RateWindow
{
    /// <summary>
    /// max number of jobs in the window
    /// </summary>
    public int Max { get; set; }

    /// <summary>
    /// how large is the window, note the larger the window the more ram will be used.
    /// </summary>
    public int SizeInSeconds { get; set; }
}