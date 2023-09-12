namespace Laters;

public class RateWindow
{
    /// <summary>
    /// this is the rate name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// max number of jobs in the window
    /// </summary>
    public int Max { get; set; }

    /// <summary>
    /// how large is the window, note the larger the window the more ram will be used.
    /// </summary>
    public int SizeInSeconds { get; set; }
}