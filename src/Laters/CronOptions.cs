namespace Laters;

public class CronOptions
{
    public Dictionary<string, string> Headers { get; set; } = new();
    public QualityDelivery Delivery { get; set; } = new();
    
    protected internal bool IsGlobalCron { get; set; } = false;
    public bool UpdateScheduledJobOnCronChange { get; set; } = true;
}