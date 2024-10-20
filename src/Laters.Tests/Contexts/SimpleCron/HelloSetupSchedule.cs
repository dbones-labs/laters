namespace Laters.Tests.Contexts.SimpleCron;

public class HelloSetupSchedule: ISetupSchedule
{
    public void Configure(IScheduleCron scheduleCron)
    {
        var every10Seconds = "*/10 * * * * *";
        scheduleCron.ManyForLater("hello", new Hello() { Name = "Bob" }, every10Seconds);
    }
}