---
outline: deep
---

# Program.cs

the main parts to make `Laters` work 

Against the `HostBuilder`

- Configure Datastore i.e. [Postgres](../storage/postgres)
- `ConfigureLaters` - configure Laters with any particular configurations you would like to apply


Against the `ApplicationBuilder`

- `UseLaters` - add the middleware that laters requires.

## HostBuilder

This is where you can set up some core fundamentals of how Laters will work.

```csharp
builder.WebHost.ConfigureLaters((context, setup) =>
{
    //override this endpoint to point at your load balancer in production/when-deployed.
    setup.Configuration.WorkerEndpoint = "http://localhost:5000/"; 
    setup.UseStorage<UseMarten>();
});
```

against the `setup` object, you can:

- Register [Handlers](../processing/job-handler)
- Register [Custom Client Actions](../processing/custom-actions)
- Register [Global Cron Jobs](../scheduling/global-many-for-later)
- Register [Storage](../storage/postgres)

### Configuration object

> [!NOTE]
> Each property has .NET comments against then.

You can use either the .NET configuration classes or code to override any default configuration that Laters uses

**Via code:**

```csharp
builder.WebHost.ConfigureLaters((context, setup) =>
{
    setup.Configuration.Role = Roles.Leader;

    //rest of laters setup
    //...
});
```

**Via .NET Config (such as appSettings):**

by calling `ConfigureLaters` with `"Laters"`, it will now load the Configuration from .NET Configuration.

```
builder.WebHost.ConfigureLaters("Laters", (context, setup) =>
{
    //override in code
    setup.Configuration.Role = Roles.Leader;
    //rest of laters setup
    //...
});
```

## ApplicationBuilder

This is where you register the middleware for `Laters` to work with `Asp.NET`

```
app.UseLaters();
```