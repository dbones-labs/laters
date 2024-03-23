---
outline: deep
---

# Quick Start.

> [!IMPORTANT]
> This is demo code, with the purpose to get you into using `Laters`

> [!NOTE]
> [full code over here](https://gist.github.com/dbones/70eaa428e8c2748959a78c2f8f4b3481)

`Laters` is designed to be fast to pick up, with its use of Minimal Api and its default use of `Marten`.

Let's build a simple Todo application (just to get you started with the API).

we want to be able to:

- create a new Todo Item.
- update the Todo Item as Completed.
- After 1 day of it being completed, remove it from the application (Laters will be used for this).

## Pre-requirement.

- Postgres
- .NET


## Nuget Packages.

The 2 default packages we need to install are

- Laters <- the core laters Library, with all the main functionality

```sh
dotnet add package Laters
```

- Laters.Marten <- the default persistence library, which provides a Unit Of Work (for full transitional support) 
 
```sh
dotnet add package Laters.Data.Marten
```
## Models.

### Simple Domain.

This is our simple todo class, the CompleteDate is indicated when the TodoItem is completed.

```csharp
public class TodoItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public bool Completed { get; set; }
}
```

### Job Model.

This class represents a work we want to queue and execute later, we only need the `Id` of the Todo Item.

```csharp
public class RemoveOldItem
{
    public string Id { get; set; } = string.Empty;
}
```

## Minimal Api.

We have tried to complement Asp.NET's Minimal Api, with a similar Api.

### ASP.NET

- 1️⃣ - In `MapPut`, we queue up the `TodoItem` for deletion using the `schedule.ForLater`

```csharp
app.MapGet("/todo-items/{id}", async ([FromQuery] string id, IQuerySession session) =>
    await session.LoadAsync<TodoItem>(id)
        is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.MapPost("/todo-items", ([FromBody] TodoItem item, IDocumentSession session) =>
{
    session.Store(item);
    return Results.Created($"/todoitems/{item.Id}", item);
});

app.MapPut("/todo-items/{id}", async (
    [FromQuery] string id, 
    [FromBody] TodoItem updateItem, 
    IDocumentSession session,
    ISchedule schedule) =>
{
    var item = await session.LoadAsync<TodoItem>(id);
    if (item is null) return Results.NotFound();

    item.Name = updateItem.Name;
    item.Details = updateItem.Details;
    item.Completed = updateItem.Completed;

    if (item.Completed)
    {
        var removeDate = SystemDateTime.UtcNow.AddDays(1);
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
app.MapHandler<RemoveOldItem>(async (JobContext<RemoveOldItem> ctx, IDocumentSession session) =>
{
    var itemId = ctx.Payload!.Id;
    session.Delete<TodoItem>(itemId);
    await Task.CompletedTask;
});
```

## Configure

there are 3 things we need to do

1. database
2. laters
3. register laters with Asp.NET

### Database (Postgres)

We have abbreviated the code here (full code), to focus on what we need to set up for `Laters` to work with Marten

```csharp
//lets setup the database
builder.Services.AddMarten(config =>
{
    //setup Marten code....
    //....

    //Setup Laters schema
    config.Schema.Include<LatersRegistry>();
});

//Laters requires dirty tracking.
builder.Services.AddScoped<IDocumentSession>(services =>
    services.GetRequiredService<IDocumentStore>().DirtyTrackedSession());
```

### Laters

`Laters` has a number of things you can modify, however, we will set up it with the minimum items

- 1️⃣ - an endpoint which is localhost 5000 (the default for ASP.NET), this can be read from config
- 2️⃣ - `UseMarten`, to make it make use of the Unit Of Work with all your ASP.NET requests.

```csharp
builder.WebHost.ConfigureLaters((context, setup) =>
{
    setup.Configuration.WorkerEndpoint = "http://localhost:5000/"; // 1️⃣
    setup.UseStorage<UseMarten>(); // 2️⃣
});
```

### ASP.NET.

> [!NOTE]
> we setup `commit` to apply `commit` on the datastore for us, in Asp.NET. This is for quickness of the demo, and should be your own middleware.

once we have built the host, we need to register `Laters` with Asp.NET

- 1️⃣ - setup laters with ASP.NET (quick setup for demo)

```csharp
var app = builder.Build();
app.UseLaters(commit: CommitStrategy.SupplyMiddleware); // 1️⃣

//setup Asp.NET
//...
```

## Run.

Okay, we should be ok to run!

```sh
dotnet run
```