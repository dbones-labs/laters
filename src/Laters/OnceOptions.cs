namespace Laters;

using Infrastucture;

public class OnceOptions
{
    public DateTime ScheduleFor { get; set; } = SystemDateTime.UtcNow;
    public Dictionary<string, string> Headers { get; set; } = new();
    public QualityDelivery Delivery { get; set; } = new();
}