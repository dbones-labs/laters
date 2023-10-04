namespace Laters.Tests.Infrastructure;

using JasperFx.Core;
using Laters.AspNet;
using Laters.Data.Marten;
using Marten;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Weasel.Core;

public class DefaultTestServer : IDisposable
{
    Action<IServiceCollection>? _configureServices;
    Action<WebHostBuilderContext, Setup>? _configureLaters;
    Action<IApplicationBuilder>? _configure;
    TestServer? _testServer;

    static Random _random = new();

    public DefaultTestServer()
    {
        Port = _random.Next(5001, 5500);
        TestNumber = _random.Next(1, 999_999);

        _configureLaters = (context, setup) =>
        {
            setup.ScanForJobHandlers();
            setup.Configuration.Role = Roles.Any;
            setup.Configuration.WorkerEndpoint = $"http://localhost:{Port}";
            setup.UseStorage<Marten>();
        };
        
        _configure = app =>
        {
            app.UseDeveloperExceptionPage();
            app.UseLaters();
            app.UseAuthorization();
        };
    }

    public async Task Inscope(Func<IAdvancedSchedule, Task> action)
    {
        using var scope = _testServer.Services.CreateScope();
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
        app.UseSwagger();
        app.UseSwaggerUI();
        _configure?.Invoke(app);
    }
    
    public void Setup()
    {
        var builder = new WebHostBuilder();
        
        builder.ConfigureLaters((context, setup) =>
        {
            setup.Configuration.NumberOfProcessingThreads = 1;
            _configureLaters?.Invoke(context, setup);
        });

        builder.ConfigureServices((context, collection) =>
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

            //quick workaround, you can also the SessionFactory
            collection.AddScoped<IDocumentSession>(services =>
                services.GetRequiredService<IDocumentStore>().DirtyTrackedSession());
            
            collection.AddSingleton<TestMonitor>();
            collection.AddControllersWithViews();
            collection.AddEndpointsApiExplorer();
            collection.AddSwaggerGen();
            
            _configureServices?.Invoke(collection);
        });
        
        
        
        builder
            .UseStartup(context => this)
            .UseUrls($"http://localhost:{Port}");
        
        _testServer = new TestServer(builder);
        
        Client = _testServer.CreateClient();
        Monitor = _testServer.Services.GetRequiredService<TestMonitor>();
        
        _testServer
            ?.Services
            .GetService<IDocumentStore>()
            ?.Advanced
            .Clean
            .CompletelyRemoveAllAsync()
            .Wait();
    }

    public void ConfigureServices(IServiceCollection configure)
    {
    }
    
    public void OverrideServices(Action<IServiceCollection> configure)
    {
        _configureServices = configure;
    }

    public void OverrideLaters(Action<WebHostBuilderContext, Setup> configure)
    {
        _configureLaters = configure;
    }
    
    public void Dispose()
    {
        _testServer?.SafeDispose();
    }
}