namespace Laters;

using Configuration;
using ServerProcessing.Windows;

public class Windows
{
    readonly IDictionary<string, RateWindow> _windows;

    protected internal Windows(IDictionary<string, RateWindow> windows)
    {
        _windows = windows;
    }

    /// <summary>
    /// configure the default window, to limit processing of a ALL jobs
    /// </summary>
    /// <param name="max"></param>
    /// <param name="sizeInSeconds"></param>
    public virtual Windows ConfigureGlobal(int max, int sizeInSeconds)
    {
        return Configure(LatersConstants.GlobalTumbler, max, sizeInSeconds);
    }

    /// <summary>
    /// configure a window, to limit processing of a set of jobs
    /// </summary>
    /// <param name="max">max number of jobs in the window</param>
    /// <param name="sizeInSeconds">how large is the window, note the larger the window the more ram will be used.</param>
    /// <typeparam name="T">the type will be converted into the name of the window</typeparam>
    public virtual Windows Configure<T>(int max, int sizeInSeconds)
    {
        return Configure(typeof(T).FullName, max, sizeInSeconds);
    }


    /// <summary>
    /// configure a window, to limit processing of a set of jobs
    /// </summary>
    /// <param name="windowName">name of the window</param>
    /// <param name="max">max number of jobs in the window</param>
    /// <param name="sizeInSeconds">how large is the window, note the larger the window the more ram will be used.</param>
    public virtual Windows Configure(string windowName, int max, int sizeInSeconds)
    {
        var exists = _windows.TryGetValue(windowName, out var window);
        if (!exists)
        {
            window = new RateWindow();
            _windows.Add(windowName, window);
        }

        window.Max = max;
        window.SizeInSeconds = sizeInSeconds;
        return this;
    }
}