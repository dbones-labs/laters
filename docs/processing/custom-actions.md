---
outline: deep
---

# Custom Actions

When you need to apply custom cross cutting code on processing jobs (i.e. `validation`, `caching` etc)

## Implementing

each action has a few things you need to todo in order to apply logic while processing jobs.

- 1️⃣ - implement the `IProcessAction<T>` interface, use the `<T>` to apply the action against any class.
- 2️⃣ - dependency injection
- 3️⃣ - implement `Execute`, which takes in 2 objects
  - `context` - this is the job that is being processed any any addition context.
  - `next` - is the delegate to invoke the next Action in the pipeline.

```csharp
using ClientProcessing;
using ClientProcessing.Middleware;
using Dbones.Pipes;

public class EpicAction<T> : IProcessAction<T> //1️⃣
{
    readonly ILogger<EpicAction<T>> _logger;

    //2️⃣
    public EpicAction(ILogger<EpicAction<T>> logger)
    {
        _logger = logger;
    }

    //3️⃣
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        _logger.LogInformation("{JobType} - Before - jobId {JobId}",
            typeof(T).FullName,
            context.JobId);

        await next(context);

        _logger.LogInformation("{JobType} - After - jobId {JobId}",
            typeof(T).FullName,
            context.JobId);
    }
}
```

Step 3️⃣, allows you to implement logic before and after the execution of the other Actions (including Handler) in the pipeline chain.

It also allows you to stop the execution the rest of the down stream pipeline items.

## Configuring

### IoC registration

> [!IMPORTANT]
> you are required to register the type directly, not aginst an interface.

> [!NOTE]
> The `Scope` is up to you, select the correct one.

You require to tell the IoC container of the type, so it can apply dependency injection correctly.

```csharp
collection.TryAddScoped(typeof(EpicAction<>));
```

### Inform the pipeline of this action

> [!WARNING]
> This is under review, however the following will work, with minimal refactoring.

We need to inform of `Laters` in which order to apply your Custom Actions, it only needs to happen at the application start, here is how you can apply this

- 1️⃣ - Apply the `ConfigureLaters` located on the `HostBuilder`
- 2️⃣ - Add to the CustomActions list (add your items in order)
- 3️⃣ - all the other setup code

```csharp

//1️⃣
builder.WebHost.ConfigureLaters((context, setup) =>
{
    //2️⃣
    setup.ClientActions.CustomActions.Add(typeof(EpicAction<>));

    //3️⃣
    setup.Configuration.WorkerEndpoint = "http://localhost:5000/";
    setup.UseStorage<UseMarten>();
    //....
});
```