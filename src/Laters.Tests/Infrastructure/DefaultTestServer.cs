namespace Laters.Tests.Infrastructure;

using Laters.AspNet;
using Laters.Data.Marten;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

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
        _testServer?.Dispose();
    }
}