namespace Laters.Tests.Infrastructure;

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
    
    public IAdvancedSchedule Schedule { get; private set; }
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

        builder.ConfigureServices((context, collection) =>
        {
            //default database
            collection.AddMarten(config =>
            {
                config.Connection("host=localhost;database=laters;password=ABC123!!;username=application");

                config.AutoCreateSchemaObjects = AutoCreate.All;
                
                config.Policies.ForAllDocuments(dm =>
                {
                    if (dm.IdType == typeof(string))
                    {
                        dm.IdStrategy = new StringIdGeneration();
                    }
                });

                config.DatabaseSchemaName = $"laters-{TestNumber}";
                
                config.CreateDatabasesForTenants(tenant =>
                {
                    tenant
                        .ForTenant()
                        .CheckAgainstPgDatabase()
                        .WithOwner("admin")
                        .WithEncoding("UTF-8")
                        .ConnectionLimit(-1);
                });
            });
            
            collection.AddSingleton<TestMonitor>();
            collection.AddControllersWithViews();
            collection.AddEndpointsApiExplorer();
            collection.AddSwaggerGen();
            _configureServices?.Invoke(collection);
        });
        
        builder.ConfigureLaters((context, setup) =>
        {
            _configureLaters?.Invoke(context, setup);
        });
        
        builder
            .UseStartup<DefaultTestServer>(context => this)
            .UseUrls($"http://localhost:{Port}");
        
        _testServer = new TestServer(builder);
        
        Client = _testServer.CreateClient();
        Schedule = _testServer.Services.GetRequiredService<IAdvancedSchedule>();
        Monitor = _testServer.Services.GetRequiredService<TestMonitor>();
    }

    public void ConfigureServices(Action<IServiceCollection> configure)
    {
        _configureServices = configure;
    }

    public void ConfigureLaters(Action<WebHostBuilderContext, Setup> configure)
    {
        _configureLaters = configure;
    }
    
    public void Dispose()
    {
        _testServer
            ?.Services
            .GetService<IDocumentStore>()
            ?.Advanced
            .Clean
            .CompletelyRemoveAllAsync()
            .Wait();
        
        _testServer?.Dispose();
    }
}