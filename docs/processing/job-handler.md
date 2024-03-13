---
outline: deep
---

# Job Handler

This is the machanism in which we apply logic for Job Types (the logic which handles the Job)

> [!NOTE]
> `Laters` uses the Job Type in order to select the correct handler. And you should consider using 1 Handler per Job Type.

## Implementing

If a job of Type `Hello`, as been queued up to be processed

each action has a few things you need to todo in order to apply logic while processing jobs.

- 1️⃣ - implement the `IJobHandler<T>` interface, where `<T>` is the Job Type
- 2️⃣ - dependency injection
- 3️⃣ - implement `Execute`, which takes in 2 objects
  - `context` - this is the job that is being processed any any addition context.

```csharp
using Laters.ClientProcessing;

public class HelloJobHandler : IJobHandler<Hello> // 1️⃣
{
    private readonly ILogger<HelloJobHandler> _logger;

    // 2️⃣
    public HelloJobHandler(ILogger<HelloJobHandler> logger)
    {
        _logger = logger;
    }
    
    // 3️⃣
    public async Task Execute(JobContext<Hello> jobContext)
    {
        var name = jobContext.Payload.Name;
        _logger.LogInformation("hello {Name}", name);
        
        await Task.CompletedTask;
    }
}
```


## Configuring

> [!NOTE]
> Handlers are registered under `Scoped` with the IoC container.


there are 2 ways to register Handlers

- AutoScanning - Reconmended (its just simplier)
- Manual 


### AutoScanning

> [!NOTE]
> Annotate any handler with `[Ignore]`, which you do not want to autoscan

This will scan the current running Application for all the handler.

- 1️⃣ - Apply the `ConfigureLaters` located on the `HostBuilder`
- 2️⃣ - `ScanForJobHandlers` will auto wire any `IJobHandler<T>` in the running application, you can provide the assembly in order to scan.

```csharp

//1️⃣
builder.WebHost.ConfigureLaters((context, setup) =>
{
    //2️⃣
    setup.ScanForJobHandlers();

    //....
});
```

### Inform the pipeline of this action

If you prefer you can wire up Handler manually, one by one.

- 1️⃣ - Apply the `ConfigureLaters` located on the `HostBuilder`
- 2️⃣ - `AddJobHandler` against any `JobHandler` you would like to register.

```csharp
//1️⃣
builder.WebHost.ConfigureLaters((context, setup) =>
{
    //2️⃣
   setup.AddJobHandler<HelloJobHandler>();;

    //....
});
```