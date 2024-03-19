---
outline: deep
---

# Minimal Api.

This is the mechanism in which we apply logic for Job Types (the logic which handles the Job)

> [!NOTE]
> `Laters` uses the Job Type in order to select the correct handler. And you should consider using 1 Handler per Job Type.

> [!NOTE]
> `Minimal Api` is another way to express a JobHandler. Consider this for smaller applications, if you want to split into a number of files see [Job Handlers](./job-handler)

## Implementing and Configuring.

> [!NOTE]
> Handlers are registered under `Scoped` with the IoC container.

If a job of Type `Hello`, has been queued up to be processed

each action has a few things you need to consider and apply logic while processing jobs.

- 1️⃣ - Apply `MapHandler` to set up a Minimal Api `JobHandler`, this has full support for Dependency Injection
    - `JobContext<Hello> ctx` parameter is the Job context, which you should include
    - order of param does not matter
- 2️⃣ - Implement logic
- 3️⃣ - `async / await` is optional (and supported)

```csharp
// 1️⃣
app.MapHandler<Hello>(async (JobContext<Hello> ctx, ILogger<Hello>) =>
{
    // 2️⃣
    var name = jobContext.Payload.Name;
    _logger.LogInformation("hello {Name}", name);
    
    // 3️⃣
    await Task.CompletedTask;
});
```