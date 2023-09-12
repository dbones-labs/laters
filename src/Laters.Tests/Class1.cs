namespace Laters.Tests;

using AspNet;
using Machine.Specifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PowerAssert;

[Subject("Later")]
class When_authenticating_an_admin_user
{
    static DefaultTestServer _testServer;

    Establish context = () =>
    {
        _testServer = new DefaultTestServer();
    };

    Because of = () =>
        hello = "hello";

    It should_indicate_the_users_role = () =>
        PAssert.IsTrue(()=> hello == "hello");

    Cleanup after = () =>
    {
        _testServer?.Dispose();
    };
}

public class DefaultTestServer : IDisposable
{
    Action<IServiceCollection>? _configureServices;
    Action<IApplicationBuilder>? _configure;
    TestServer? _testServer;

    static Random _random = new();

    public DefaultTestServer()
    {
        _configure = app =>
        {
            app.UseDeveloperExceptionPage();
            app.UseLaters();
            app.UseAuthorization();
        };
        
        Port = _random.Next(5001, 5500);
    }
    
    public IAdvancedSchedule Schedule { get; private set; }
    public HttpClient Client { get; set; }

    public int Port { get; private set; }
    
    public void Configure(IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        _configure?.Invoke(app);
    }
    
    public void Create()
    {
        var builder = new WebHostBuilder();

        builder.ConfigureLaters(config =>
        {
            config.ScanForJobHandlers();
//            config.Windows.Default;
            config.Windows.Configure()
            config.UseStorage<Marten>();
        });
        
        builder.ConfigureServices((context, collection) =>
        {
            collection.AddControllersWithViews();
            collection.AddEndpointsApiExplorer();
            collection.AddSwaggerGen();
            _configureServices?.Invoke(collection);
        });

        builder
            .UseStartup<DefaultTestServer>(context => this)
            .UseUrls($"http://localhost:{Port}");
        
        _testServer = new TestServer(builder);
        Client = _testServer.CreateClient();
    }

    public void ConfigureServices(Action<IServiceCollection> configure)
    {
        _configureServices = configure;
    }
    
    public void Dispose()
    {
        _testServer?.Dispose();
    }
}