﻿namespace Laters.Tests.Infrastructure;

using JasperFx.Core;
using AspNet;
using Laters.Configuration;
using Laters.Data.Marten;
using Marten;
using Marten.Linq.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Weasel.Core;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Filters;
using Serilog.Sinks.OpenTelemetry;

public class DefaultTestServer : IDisposable
{
    Action<IServiceCollection>? _configureServices;
    Action<WebHostBuilderContext, Setup>? _configureLaters;
    Action<IApplicationBuilder>? _configure;
    //TestServer? _testServer;
    WebApplication _server;

    static Random _random = new();

    public DefaultTestServer()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        Port = _random.Next(5001, 5500);
        TestNumber = _random.Next(1, 999_999);

        _configureLaters = (context, setup) =>
        {
            setup.ScanForJobHandlers();
            setup.Configuration.Role = Roles.Any;
            setup.Configuration.WorkerEndpoint = $"http://localhost:{Port}/";
            setup.UseStorage<Marten>();
        };
        
        _configure = app =>
        {
            app.UseLaters();
            //app.UseAuthorization();
        };
    }

    
    public async Task InScope<T>(Func<IAdvancedSchedule, T> action)
    {
        using var scope = _server.Services.CreateScope();
        using var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        var schedule = scope.ServiceProvider.GetRequiredService<IAdvancedSchedule>();
        action(schedule);
        await documentSession.SaveChangesAsync();
    } 
    
    public async Task InScope(Func<IAdvancedSchedule, Task> action)
    {
        using var scope = _server.Services.CreateScope();
        using var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        var schedule = scope.ServiceProvider.GetRequiredService<IAdvancedSchedule>();
        await action(schedule);
        await documentSession.SaveChangesAsync();
    } 
    public HttpClient Client { get; private set; }
    public int Port { get; private set; }

    public int TestNumber { get; set; }

    public TestMonitor Monitor { get; set; }
    
    public void Configure(IApplicationBuilder app)
    {
        throw new Exception("asdsads");
        //app.UseSwagger();
        //app.UseSwaggerUI();
        app.UseHttpLogging();
        app.UseRouting();
        app.UseDeveloperExceptionPage();
        _configure?.Invoke(app);
    }
    
    /// <summary>
    /// call this to build the app and run it.
    /// </summary>
    public void Setup()
    {
        var builder = WebApplication.CreateBuilder();
        
        builder.Host.UseSerilog((context, config) =>
        {
            config
                .Enrich.FromLogContext()
                .
                .Enrich.With(new PropertyEnricher("test_number", $"{TestNumber}"))
                .Enrich.With(new PropertyEnricher("service_name","Laters"))
                .Filter.ByIncludingOnly(Matching.FromSource("Laters"))
                .WriteTo.OpenTelemetry(opt =>
                {
                    opt.IncludedData = IncludedData.SpanIdField | 
                                       IncludedData.TraceIdField |
                                       IncludedData.TemplateBody;
                })
                .WriteTo.Console()
                .MinimumLevel.Information();
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(b => b.AddPrometheusExporter())
            .WithTracing(b =>
            {
                b.AddSource("Laters")
                    .ConfigureResource(r => r.AddService("Laters"))
                    .AddAspNetCoreInstrumentation()
                    .AddNpgsql()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter();
            });
        
        builder.WebHost.ConfigureLaters((context, setup) =>
        {
            setup.Configuration.NumberOfProcessingThreads = 1;
            _configureLaters?.Invoke(context, setup);
        });

        builder.WebHost.ConfigureServices((context, collection) =>
        {
            //default database
            collection.AddMarten(config =>
            {
                var connectionString = "host=localhost;database=laters;password=ABC123!!;username=application";
                config.Connection(connectionString);

                config.AutoCreateSchemaObjects = AutoCreate.All;
                
                // config.Policies.ForAllDocuments(dm =>
                // {
                //     if (dm.IdType == typeof(string))
                //     {
                //         dm.IdStrategy = new StringIdGeneration();
                //     }
                // });
                
                
                config.Schema.Include<LatersRegistry>();

                config.DatabaseSchemaName = "thisisatest";
                
                config.CreateDatabasesForTenants(tenant =>
                {
                    tenant.MaintenanceDatabase(connectionString);
                    tenant
                        .ForTenant()
                        .CheckAgainstPgDatabase()
                        .WithOwner("admin")
                        .WithEncoding("UTF-8")
                        .ConnectionLimit(-1)
                        .OnDatabaseCreated(_ => { });;
                });
            });

            collection.AddHostedService<ResetDataService>();
            
            //quick workaround, you can also the SessionFactory
            collection.AddScoped<IDocumentSession>(services =>
                services.GetRequiredService<IDocumentStore>().DirtyTrackedSession());
            
            collection.AddSingleton<TestMonitor>();
            collection.AddControllersWithViews();
            collection.AddEndpointsApiExplorer();
            collection.AddSwaggerGen();
            
           
            collection.AddLogging();
            
            _configureServices?.Invoke(collection);
        });


        builder.WebHost
            .UseUrls($"http://localhost:{Port}/");
        
        _server = builder.Build();
        
        //add some default middleware
        _server.UseHttpLogging();
        _server.UseRouting();
        _server.UseDeveloperExceptionPage();
        _server.UseSwagger();
        _server.UseSwaggerUI();
        _server.UseOpenTelemetryPrometheusScrapingEndpoint();
        
        _configure?.Invoke(_server);
        
        _server.RunAsync();
        
        //_testServer = new TestServer(builder);

        Client = _server.Services.GetRequiredService<HttpClient>();
        
        //Client = _testServer.CreateClient();
        Monitor = _server.Services.GetRequiredService<TestMonitor>();
    }

    public void ConfigureServices(IServiceCollection configure)
    {
    }
    
    public void OverrideServices(Action<IServiceCollection> configure)
    {
        _configureServices = configure;
    }
    
    public void OverrideBuilder(Action<IApplicationBuilder> configure)
    {
        _configure = configure;
    }

    /// <summary>
    /// this is
    /// </summary>
    /// <param name="configure"></param>
    public void OverrideLaters(Action<WebHostBuilderContext, Setup> configure)
    {
        _configureLaters = configure;
    }
    
    public void Dispose()
    {
        _server?.SafeDispose();
    }
}