using Laters;
using Laters.AspNet;
using Laters.ClientProcessing;
using Laters.Data.Marten;
using Laters.Minimal.Application;
using Marten;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureServices((context, collection) =>
{
    //lets setup the database
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    collection.AddMarten(config =>
    {
        //read this from config.. but for now...
        var connectionString = "host=localhost;database=laters;password=ABC123!!;username=application";
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
    collection.AddScoped<IDocumentSession>(services =>
        services.GetRequiredService<IDocumentStore>().DirtyTrackedSession());
});


builder.WebHost.ConfigureLaters((context, setup) =>
{
    setup.Configuration.WorkerEndpoint = "http://localhost:5000/";
    setup.UseStorage<UseMarten>();
});

var app = builder.Build();

//setup laters with aspnet (quick setup for demo)
app.UseLaters(commit: CommitStrategy.SupplyMiddleware);

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

    var shouldRemoveJob = item.RemoveJobId is not null && !updateItem.CompletedDate.HasValue;
    var shouldAddJob = !updateItem.CompletedDate.HasValue;
    
    item.Details = updateItem.Details;
    item.CompletedDate = updateItem.CompletedDate;

    if (shouldRemoveJob)
    {
        schedule.ForgetAboutIt<RemoveOldItem>(item.RemoveJobId!);
    }

    if (shouldAddJob)
    {
        var removeDate = item.CompletedDate!.Value.AddDays(1);
        var jobId = schedule.ForLater(new RemoveOldItem { Id = item.Id }, removeDate);
        item.RemoveJobId = jobId;
    }
    
    return Results.NoContent();
});


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
    public string Id { get; set; }
    public string Details { get; set; }
    public DateTime? CompletedDate { get; set; }

    public string? RemoveJobId { get; set; }
}

public class RemoveOldItem
{
    public string Id { get; set; }
}