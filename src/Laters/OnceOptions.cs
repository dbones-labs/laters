namespace Laters;

public class OnceOptions
{
    public DateTime ScheduleFor { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Headers { get; set; } = new();
    public QualityDelivery Delivery { get; set; } = new();
}