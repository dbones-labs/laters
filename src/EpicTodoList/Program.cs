using System.ComponentModel.DataAnnotations;
using Laters;
using Laters.AspNet;
using Laters.ClientProcessing;
using Laters.Data.Marten;
using Laters.Minimal.Application;
using Marten;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

//lets setup the database
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddMarten(config =>
{
    //read this from config.. but for now...
    var connectionString = "host=postgres;database=laters;password=ABC123!!;username=application";
    config.Connection(connectionString);
    config.AutoCreateSchemaObjects = AutoCreate.All;
    config.DatabaseSchemaName = "todoapp";

    config.CreateDatabasesForTenants(tenant =>
    {
        tenant.MaintenanceDatabase(connectionString);
        tenant
            .ForTenant()
            .CheckAgainstPgDatabase()
            .WithOwner("admin")
            .WithEncoding("UTF-8")
            .ConnectionLimit(-1)
            .OnDatabaseCreated(_ => { });
    });

    //Setup Laters State!
    config.Schema.Include<LatersRegistry>();

});

//quick workaround, you can also the SessionFactory
builder.Services.AddScoped<IDocumentSession>(services =>
    services.GetRequiredService<IDocumentStore>().DirtyTrackedSession());

builder.WebHost.ConfigureLaters((context, setup) =>
{
    setup.Configuration.WorkerEndpoint = "http://localhost:5000/";
    setup.UseStorage<UseMarten>();
});

var app = builder.Build();

//setup laters with aspnet (quick setup for demo)
app.UseLaters(commit: CommitStrategy.SupplyMiddleware);

//minimal http
app.MapGet("/todo-items", (IQuerySession session) =>
{
    var items = session.Query<TodoItem>();
    return Task.FromResult(items);
});

app.MapGet("/todo-items/{id}", async (string id, IQuerySession session) =>
    await session.LoadAsync<TodoItem>(id)
        is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.MapPost("/todo-items", (TodoItem item, IDocumentSession session) =>
{
    session.Store(item);
    return Results.Created($"/todo-items/{item.Id}", item);
});

app.MapPut("/todo-items/{id}", async (
    string id,
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
        schedule.ForLater(new RemoveOldItem { Id = item.Id }, removeDate);
    }

    return Results.NoContent();
});

//minimal laters
app.MapHandler<RemoveOldItem>(async (JobContext<RemoveOldItem> ctx, IDocumentSession session) =>
{
    var itemId = ctx.Payload!.Id;
    session.Delete<TodoItem>(itemId);
    await Task.CompletedTask;
});

app.Run();

//model
public class TodoItem
{
    [Required(AllowEmptyStrings = false)]
    public string Id { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Details { get; set; } = string.Empty;

    public DateTime? CompletedDate { get; set; }

    public string? RemoveJobId { get; set; }
}

public class RemoveOldItem
{
    public string Id { get; set; } = string.Empty;
}