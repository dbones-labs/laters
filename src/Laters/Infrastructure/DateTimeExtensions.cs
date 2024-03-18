namespace Laters.Infrastructure;

public static class DateTimeExtensions 
{
    
    /// <summary>
    /// this will truncate a datetime to a provided precision
    /// </summary>
    /// <param name="dateTime">the value to apply against</param>
    /// <param name="timeSpan">the precision to apply</param>
    /// <returns>a new datetime, which the precision applied</returns>
    /// <remarks>
    /// https://stackoverflow.com/a/1005222
    /// </remarks>
    public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
    {
        if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
        if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue) return dateTime; // do not modify "guard" values
        return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
    }
}