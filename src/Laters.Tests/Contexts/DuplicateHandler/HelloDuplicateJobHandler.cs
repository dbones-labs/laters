namespace Laters.Tests.Contexts.DuplicateHandler;

using Laters.ClientProcessing;
using Laters.Configuration;
using Laters.Tests.Infrastructure;
using Simple;

[Ignore]
public class HelloDuplicateJobHandler : IJobHandler<Hello>
{
    private readonly TestMonitor _monitor;

    public HelloDuplicateJobHandler(TestMonitor monitor)
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

