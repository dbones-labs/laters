namespace Laters.Tests.Contexts.Simple;

using ClientProcessing;
using Infrastructure;
using Models;

public class Bye : Entity
{
    public string Name { get; set; } = string.Empty;
}

public class ByeJobHandler : IJobHandler<Bye>
{
    private readonly TestMonitor _monitor;

    public ByeJobHandler(TestMonitor monitor)
    {
        _monitor = monitor;
    }
    
    public Task Execute(JobContext<Bye> jobContext)
    {
        var name = jobContext.Payload.Name;
        Console.WriteLine($"good bye {name}");
        
        _monitor.AddCallTick(this);
        return Task.CompletedTask;
    }
}