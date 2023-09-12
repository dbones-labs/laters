using Monitor = Laters.Tests.Infrastructure.TestMonitor;

namespace Laters.Tests.Contexts.SimpleCron;

public class Hello
{
    public string Name { get; set; } = string.Empty;
}

public class HelloJobHandler : IJobHandler<Hello>
{
    private readonly Monitor _monitor;

    public HelloJobHandler(Monitor monitor)
    {
        _monitor = monitor;
    }
    
    public Task Execute(JobContext<Hello> jobContext)
    {
        _monitor.AddCallTick(this);
        return Task.CompletedTask;
    }
}

public class HelloSetupSchedule: ISetupSchedule
{
    public void Configure(IScheduleCron scheduleCron)
    {
        var every10Seconds = "*/10 * * * * *";
        scheduleCron.ManyForLater("hello", new Hello(), every10Seconds);
    }
}