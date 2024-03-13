---
outline: deep
---

# Quick Start

> [!IMPORTANT]
> This is demo code, with the purpose to get you into using `Laters`

`Laters` is designed to be fast to pick up, with its use of Minimal Api and its default use of `Marten`

Lets build a simple todo application (just to get you started with the API)

we want to be able to

- create a new Todo Item
- update the Todo Item as Completed
- After 1 day of it being completed, remove it from the application (Laters will be used for this)

## Pre-requirement

- Postgres
- .NET


## Nuget Packages

The 2 default packages we need to install are

- Laters <- the core laters Library, with all the main functionality

```sh
dotnet add package Laters
```

- Laters.Marten <- the default persistence library, which proivdies a Unit Of Work (for full transiational support) 
 
```sh
dotnet add package Laters.Marten
```
## Models

### Simple Domain

This is our simple todo class, the CompleteDate is indicate when the TodoItem is completed.

```csharp
public class TodoItem
{
    public string Id { get; set; }
    public string Details { get; set; }
    public DateTime? CompletedDate { get; set; }
}
```

### Job Model

This class represent a work we want to queue and execute later, we only need the Id of the Todo Item.

```csharp
public class RemoveOldItem
{
    public string Id { get; set; }
}
```

## Minimal Api

We have tried to compliment Asp.NET's Minimal Api, with a similar Api

### Asp.NET

- 1️⃣ - In the `MapPut`, we queue up the todo item for deletion using `schedule.ForLater`

```csharp
app.MapGet("/todoitems/{id}", async (int id, IQuerySession session) =>
    await session.LoadAsync<TodoItem>(id)
        is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.MapPost("/todoitms", (TodoItem item, IDocumentSession session) =>
{
    session.Store(item);
    return Results.Created($"/todoitems/{item.Id}", item);
});

app.MapPut("/todoitems/{id}", async (
    int id, 
    TodoItem updateItem, 
    IDocumentSession session,
    ISchedule schedule) =>
{
    var item = await session.LoadAsync<TodoItem>(id);
    if (item is null) return Results.NotFound();

    item.Details = updateItem.Details;
    item.CompletedDate = updateItem.CompletedDate;

    if (item.CompletedDate.HasValue)
    {
        var removeDate = item.CompletedDate!.Value.AddDays(1);
        schedule.ForLater(new RemoveOldItem { Id = item.Id }, removeDate); // 1️⃣
    }

    return Results.NoContent();
});
```

### Laters

> [!NOTE]
> the Unit of Work's `Commit` is applied in the client pipeline

Here is the code we apply to delete the todo item, once the day has gone by.

```csharp
app.MapHandler<RemoveOldItem>((
    JobContext<RemoveOldItem> ctx,
    IDocumentSession session) =>
{
    var itemId = ctx.Payload!.Id;
    session.Delete<TodoItem>(itemId);
});
```

## Configure

there are 3 things we need to do

1. database
2. laters
3. register laters with Asp.NET

### Database (Postgres)

We have abbrivated the code here (full code), to the main lines we need to setup inorder to make `Laters` work with Marten

```csharp
builder.WebHost.ConfigureServices((context, collection) =>
{
    //lets setup the database
    collection.AddMarten(config =>
    {
        //setup Marten code....
        //....

        //Setup Laters schema
        config.Schema.Include<LatersRegistry>();
    });

    //Laters requires dirty tracking.
    collection.AddScoped<IDocumentSession>(services =>
        services.GetRequiredService<IDocumentStore>().DirtyTrackedSession());
});
```

### Laters

`Laters` has a number of things you can modify, however we will setup it with the minimum items

- 1️⃣ - an endpoint which is localhost 5000 (the default for Asp.NET), this can be read from config
- 2️⃣ - `UseMarten`, to make it make use of the Unit Of Work with all you Asp.NET requests.

```csharp
builder.WebHost.ConfigureLaters((context, setup) =>
{
    setup.Configuration.WorkerEndpoint = "http://localhost:5000/"; // 1️⃣
    setup.UseStorage<UseMarten>(); // 2️⃣
});
```

### Asp.NET

> [!NOTE]
> we setup `commit` to apply `commit` on the datastore for us, in Asp.NET. This is for quickness of the demo, and should be your own middleware.

once we have built the host, we need to register `Laters` with Asp.NET

- 1️⃣ - setup laters with aspnet (quick setup for demo)

```csharp
var app = builder.Build();
app.UseLaters(commit: CommitStrategy.SupplyMiddleware); // 1️⃣

//setup Asp.NET
//...
```

## Run

Ok we should be ok to run!

```sh
dotnet run
```