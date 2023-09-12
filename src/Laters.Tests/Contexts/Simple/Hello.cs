using Monitor = Laters.Tests.Infrastructure.TestMonitor;

namespace Laters.Tests.Contexts.Simple;

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