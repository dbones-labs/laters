namespace Laters.Tests.Contexts.SimpleCron;

using ClientProcessing;
using Infrastructure;

public class HelloJobHandler : IJobHandler<Hello>
{
    private readonly TestMonitor _monitor;

    public HelloJobHandler(TestMonitor monitor)
    {
        _monitor = monitor;
    }
    
    public Task Execute(JobContext<Hello> jobContext)
    {
        _monitor.AddCallTick(this);
        return Task.CompletedTask;
    }
}