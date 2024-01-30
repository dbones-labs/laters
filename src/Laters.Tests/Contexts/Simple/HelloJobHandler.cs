namespace Laters.Tests.Contexts.Simple;

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
        var name = jobContext.Payload.Name;
        Console.WriteLine($"hello {name}");
        
        _monitor.AddCallTick(this);
        return Task.CompletedTask;
    }
}