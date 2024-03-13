---
outline: deep
---

# Postgres

`Postgres` is supported out of the box with `Marten`, we have written a provider to make it work with `Laters`

## Configuration

### HostBuilder - Marten

- 1️⃣ - `LatersRegistry` will inform Maren of the Schema we require for Laters to work (based on our [model](../overview/model.md))
- 2️⃣ - we require `DirtyTracking`, and optionally we support 2 connection strings if you want to provide support for `IQuerySession` (this can improve performance of your architecture)

```csharp
builder.WebHost.ConfigureServices((context, collection) =>
{
    //lets setup the database
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    collection.AddMarten(config =>
    {
        //configure marten
        //....

        // 1️⃣  
        config.Schema.Include<LatersRegistry>();

    });

    // 2️⃣
    collection.AddScoped<IDocumentSession>(services =>
        services.GetRequiredService<IDocumentStore>().DirtyTrackedSession());
});
```

### HostBuilder - Laters

- 1️⃣ - pass in `UseMarten` to the `UseStorage` method, and this will setup the storage

```csharp
builder.WebHost.ConfigureLaters((context, setup) =>
{
    //configure Laters
    //...

    // 1️⃣
    setup.UseStorage<UseMarten>(); 
});
```


