namespace Laters.Tests.Infrastructure;

using JasperFx.Core;
using AspNet;
using Infrastucture.Telemetry;
using Laters.Configuration;
using Laters.Data.Marten;
using Marten;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Weasel.Core;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Filters;
using Serilog.Sinks.OpenTelemetry;
using ServerProcessing;

public class DefaultTestServer : IDisposable
{
    readonly Roles _role;
    readonly TestData _data;
    Action<IServiceCollection>? _configureServices;
    Action<WebHostBuilderContext, Setup> _defaultConfigureLaters;
    Action<WebHostBuilderContext, Setup> _configureLaters;
    Action<IApplicationBuilder>? _minimalApiConfigure;
    Action<IApplicationBuilder>? _configure;
    //TestServer? _testServer;
    WebApplication _server;

    static Random _random = new();

    public DefaultTestServer(int port = 0, Roles role = Roles.All, TestData data = TestData.Clear)
    {
        _role = role;
        _data = data;
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        Port = port == 0 ? _random.Next(5001, 5500) : port;
        TestNumber = _random.Next(1, 999_999);

        _defaultConfigureLaters = (context, setup) =>
        {
            setup.ScanForJobHandlers();
            setup.Configuration.Role = _role;
            setup.Configuration.WorkerEndpoint = $"http://localhost:{Port}/";
            setup.UseStorage<UseMarten>();
        };
        
        _configureLaters = _defaultConfigureLaters;

        _minimalApiConfigure = builder => { };

        _configure = app =>
        {
            app.UseLaters();
        };
    }

    public HttpClient Client { get; private set; }
    public int Port { get; init; }

    public int TestNumber { get; init; }

    public TestMonitor Monitor { get; private set; }

    public LeaderContext Leader { get; private set; }
    
    
    public async Task InScope(Action<IAdvancedSchedule> action)
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
    
    
    /// <summary>
    /// call this to build the app and run it.
    /// </summary>
    public async Task Setup()
    {
        var builder = WebApplication.CreateBuilder();
        
        builder.Host.UseSerilog((context, config) =>
        {
            config
                .Enrich.FromLogContext()
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
                .MinimumLevel.Debug();
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(b => b.AddPrometheusExporter())
            .WithTracing(b =>
            {
                b.AddSource(Telemetry.Name);
                b.AddSource("Laters")
                    .ConfigureResource(r => r.AddService("Laters"))
                    .AddAspNetCoreInstrumentation()
                    .AddNpgsql()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter();
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

            //we need to clear the data before we setup laters
            if (_data == TestData.Clear)
            {
                collection.AddHostedService<ResetDataService>();
            }
            
            builder.WebHost.ConfigureLaters((context, setup) =>
            {
                setup.Configuration.NumberOfProcessingThreads = 1;
                _configureLaters?.Invoke(context, setup);
            });
            
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
        _minimalApiConfigure?.Invoke(_server);
        
        _server.RunAsync(); //we do not want to be blocking
        await Task.Delay(100); //but we will allow a tiny bit of time to setup
        
        
        //_testServer = new TestServer(builder);

        Client = _server.Services.GetRequiredService<HttpClient>();
        
        //Client = _testServer.CreateClient();
        Monitor = _server.Services.GetRequiredService<TestMonitor>();
        
        Leader = _server.Services.GetRequiredService<LeaderContext>();
    }
    
    public void OverrideServices(Action<IServiceCollection> configure)
    {
        _configureServices = configure;
    }
    
    public void OverrideBuilder(Action<IApplicationBuilder> configure)
    {
        _configure = configure;
    }
    
    public void MinimalApi(Action<IApplicationBuilder> configure)
    {
        _minimalApiConfigure = configure;
    }

    /// <summary>
    ///  override all the test defaults for laters
    /// </summary>
    /// <param name="configure"></param>
    public void OverrideLaters(Action<WebHostBuilderContext, Setup> configure)
    {
        _configureLaters = configure;
    }
    
    /// <summary>
    /// add more configuration for laters
    /// </summary>
    /// <param name="configure"></param>
    public void AdditionalOverrideLaters(Action<WebHostBuilderContext, Setup> configure)
    {
        _configureLaters = (context, setup) =>
        {
            _defaultConfigureLaters(context, setup);
            configure(context,setup);
        };
    }
    
    public void Dispose()
    {
        _server?.SafeDispose();
    }

    #region DoNotUse
    
    public void ConfigureServices(IServiceCollection configure)
    {
    }
    
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

    #endregion
    
}

public enum TestData
{
    Clear,
    Keep
}