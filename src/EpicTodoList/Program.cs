using System.ComponentModel.DataAnnotations;
using Laters;
using Laters.AspNet;
using Laters.ClientProcessing;
using Laters.Configuration;
using Laters.Data.Marten;
using Laters.Infrastructure;
using Laters.Infrastructure.Telemetry;
using Laters.Minimal.Application;
using Marten;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Sinks.OpenTelemetry;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);



builder.Host.UseSerilog((context, config) =>
{
    config
        .Enrich.FromLogContext()
        .Enrich.With(new PropertyEnricher("service_name", "Laters"))
        //.Filter.ByIncludingOnly(Matching.FromSource("Laters"))
        .WriteTo.OpenTelemetry(opt =>
        {
            opt.Endpoint = "http://otel-collector:4317";

            opt.IncludedData = IncludedData.SpanIdField |
                                IncludedData.TraceIdField |
                                IncludedData.TemplateBody;
        })
        .WriteTo.Console()
        .MinimumLevel.Information();
});

builder.Services.AddOpenTelemetry()
    .WithMetrics(b =>
    {
        b.ConfigureResource(r => r.AddService("Laters"))
            .AddMeter(Telemetry.Name)
            .AddProcessInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddPrometheusExporter();
    }).WithTracing(b =>
    {
        b.AddSource(Telemetry.Name);
        b.AddSource("Laters")
            .ConfigureResource(r => r.AddService("Laters"))
            .AddAspNetCoreInstrumentation()
            .AddNpgsql()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri("http://otel-collector:4317");
            });
    });



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
    setup.Configuration.Windows.TryAdd("quick", new RateWindow()
    {
        Max = 1_000_000,
        SizeInSeconds = 1
    });

    setup.Configuration.Windows.TryAdd("slow", new RateWindow()
    {
        Max = 500,
        SizeInSeconds = 1
    });

    setup.Configuration.InMemoryWorkerQueueMax = 1000;
    setup.Configuration.NumberOfProcessingThreads = 16;
    setup.ScanForCronSetups();
    setup.Configuration.WorkerEndpoint = "http://localhost:5235/";
    setup.UseStorage<UseMarten>();
});

var app = builder.Build();

//setup laters with aspnet (quick setup for demo)
app.UseLaters(commit: CommitStrategy.SupplyMiddleware);
app.MapPrometheusScrapingEndpoint();

//minimal http
app.MapGet("/todo-items", (IQuerySession session) =>
{
    var items = session.Query<TodoItem>();
    return Task.FromResult(items);
});

app.MapGet("/todo-items/{id}", async ([FromQuery] string id, IQuerySession session) =>
    await session.LoadAsync<TodoItem>(id)
        is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.MapPost("/todo-items", ([FromBody] TodoItem item, IDocumentSession session) =>
{
    session.Store(item);
    return Results.Created($"/todo-items/{item.Id}", item);
});

app.MapPut("/todo-items/{id}", async (
    [FromRoute] string id,
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
        var removeDate = SystemDateTime.UtcNow.AddSeconds(2);
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


var rnd = new Random();

app.MapHandler<SetupTasks>(async (ISchedule schedule, IDocumentSession session) =>
{
    var randomBetween1And1000 = rnd.Next(1, 20);
    for (var i = 0; i < randomBetween1And1000; i++)
    {
        var item = new TodoItem
        {
            Id = Guid.NewGuid().ToString("D"),
            Name = $"Task {i}",
            Details = $"Details for Task {i}"
        };

        session.Store(item);

        if (i % 4 == 0)
        {
            var options = new OnceOptions();
            options.Delivery.WindowName = "quick";
            schedule.ForLater(new SetupDoneIn5 { Id = item.Id }, SystemDateTime.UtcNow.AddMinutes(20), options);

        }
        else
        {
            var options = new OnceOptions();
            options.Delivery.WindowName = "slow";
            schedule.ForLater(new SetupDoneIn3 { Id = item.Id }, SystemDateTime.UtcNow.AddHours(3), options);
        }

    }

    await Task.CompletedTask;
});


app.MapHandler<SetupDoneIn3>(async (JobContext<SetupDoneIn3> ctx, ISchedule schedule, IDocumentSession session) =>
{
    schedule.ForLater(new SetupDone { Id = ctx.JobId });
    await Task.CompletedTask;
});

app.MapHandler<SetupDoneIn5>(async (JobContext<SetupDoneIn5> ctx, ISchedule schedule, IDocumentSession session) =>
{
    schedule.ForLater(new SetupDone { Id = ctx.JobId });
    await Task.CompletedTask;
});

app.MapHandler<SetupDone>(async (JobContext<SetupDone> ctx, ISchedule schedule, IDocumentSession session) =>
{
    var item = await session.LoadAsync<TodoItem>(ctx.Payload.Id);
    if (item is null) return;

    item.Completed = true;
    var removeDate = SystemDateTime.UtcNow.AddSeconds(2);
    schedule.ForLater(new RemoveOldItem { Id = item.Id }, removeDate);
});


app.Run();

//model


public class GlobalJobs : ISetupSchedule
{
    public void Configure(IScheduleCron scheduleCron)
    {
        var everyMinute = "* * * * *";
        scheduleCron.ManyForLater("global-everyMinute", new SetupTasks(), everyMinute);
    }

}


public class SetupTasks { }

public class SetupDoneIn3
{
    public string Id { get; set; } = string.Empty;
}

public class SetupDoneIn5
{
    public string Id { get; set; } = string.Empty;
}

public class SetupDone
{
    public string Id { get; set; } = string.Empty;
}


public class TodoItem
{
    [Required(AllowEmptyStrings = false)]
    public string Id { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Details { get; set; } = string.Empty;

    public bool Completed { get; set; }

}

/// <summary>
/// This is a simple job that will remove a <see cref="TodoItem"/> from the database
/// </summary>
public class RemoveOldItem
{
    public string Id { get; set; } = string.Empty;
}