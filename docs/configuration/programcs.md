---
outline: deep
---

# Program.cs

the main parts to make `Laters` works 

Against the `HostBuilder`

- Configure Datastore i.e. [Postgres](../storage/postgres)
- `ConfigureLaters` - configure Laters with any particular configurations you would like to apply


Against the `ApplicationBuilder`

- `UseLaters` - add the middleware that laters requires.

## HostBuilder

This is where you can setup some core fundimentals of how Laters will work.

```csharp
builder.WebHost.ConfigureLaters((context, setup) =>
{
    //overide this endpoint to point at your loadbalancer in production/when-deployed.
    setup.Configuration.WorkerEndpoint = "http://localhost:5000/"; 
    setup.UseStorage<UseMarten>();
});
```

against the `setup` object you can

- Register [Handlers](../processing/job-handler)
- Register [Custom Client Actions](../processing/custom-actions)
- Register [Global Cron Jobs](../scheduling/global-many-for-later)
- Register [Storage](../storage/postgres)

### Configuration object

> [!NOTE]
> Each propery has .NET comments against then.

You can use either the .NET configurtion classes or code to override any default configuraion that Laters uses

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

This is where you setup the middleware for `Laters` to work with `Asp.NET`

```
app.UseLaters();
```




> [!NOTE]
> `Laters` checks the datastore every 3 seconds, this can be overriden.

This mechanism allows you to queue work (a single instance) to be processed now on another process/thread, or delay it to laters (to a datetime).

there are a few commands we can do, here are some to give you a feel of the API.

## ForLater - ASAP