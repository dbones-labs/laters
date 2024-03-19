namespace Laters;

using Configuration;
using Infrastructure;
using ServerProcessing.Windows;

public static class WindowsExtensions
{
    /// <summary>
    /// configure the default window, to limit processing of a ALL jobs
    /// </summary>
    /// <param name="max"></param>
    /// <param name="sizeInSeconds"></param>
    public static IDictionary<string, RateWindow> ConfigureGlobal(this IDictionary<string, RateWindow> windows, int max,
        int sizeInSeconds)
    {
        return windows.Configure(LatersConstants.GlobalTumbler, max, sizeInSeconds);
    }

    /// <summary>
    /// configure a window, to limit processing of a set of jobs
    /// </summary>
    /// <param name="max">max number of jobs in the window</param>
    /// <param name="sizeInSeconds">how large is the window, note the larger the window the more ram will be used.</param>
    /// <typeparam name="T">the type will be converted into the name of the window</typeparam>
    public static IDictionary<string, RateWindow> Configure<T>(this IDictionary<string, RateWindow> windows, int max,
        int sizeInSeconds)
    {
        return windows.Configure(typeof(T).FullName, max, sizeInSeconds);
    }


    /// <summary>
    /// configure a window, to limit processing of a set of jobs
    /// </summary>
    /// <param name="windowName">name of the window</param>
    /// <param name="max">max number of jobs in the window</param>
    /// <param name="sizeInSeconds">how large is the window, note the larger the window the more ram will be used.</param>
    public static IDictionary<string, RateWindow> Configure(this IDictionary<string, RateWindow> windows,
        string windowName, int max, int sizeInSeconds)
    {
        var exists = windows.TryGetValue(windowName, out var window);
        if (!exists)
        {
            window = new RateWindow();
            windows.Add(windowName, window);
        }

        window.Max = max;
        window.SizeInSeconds = sizeInSeconds;
        return windows;
    }
}