namespace Laters.Infrastructure;

public static class SystemDateTime
{
    private static Func<DateTime> _getDateTime = () => DateTime.UtcNow;

    public static void Reset() => _getDateTime = () => DateTime.UtcNow;

    public static void Set(Func<DateTime> apply) => _getDateTime = apply;
    
    public static DateTime UtcNow => _getDateTime();
}