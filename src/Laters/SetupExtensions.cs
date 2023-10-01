namespace Laters;

using System.Reflection;
using System.Runtime.CompilerServices;
using Engine;
using Microsoft.Extensions.DependencyInjection.Extensions;

public abstract class StorageSetup
{
    protected internal abstract void Apply(IServiceCollection serviceCollection);
}

public class Setup
{
    List<Type> _jobHandlerTypes = new();
    StorageSetup _storageSetup;

    internal void Apply(IServiceCollection serviceCollection)
    {
        if (_storageSetup == null) throw new Exception("Please setup a storage");
        _storageSetup.Apply(serviceCollection);

        foreach (var type in _jobHandlerTypes)
        {
            serviceCollection.AddScoped(type);
        }
    }

    public void ScanForJobHandlers(Assembly? fromHere = null)
    {
        var jobHandlerType = typeof(IJobHandler<>);

        fromHere ??= Assembly.GetCallingAssembly();
        var types = fromHere.GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract)
            .Select(x => new
            {
                JobType = GetImplementedType(x, jobHandlerType),
                HandlerType = x
            })
            .Where(x => x.JobType is not null);

        _jobHandlerTypes = types
            .Select(x => x.HandlerType)
            .Union(_jobHandlerTypes)
            .Distinct()
            .ToList();
    }

    public Windows Windows { get; set; }
    public LatersConfiguration Configuration { get; set; }
    public IConfigurationSection ConfigurationSection { get; set; }

    static Type? GetImplementedType(Type svcType, Type jobHandlerType)
    {
        if (svcType.IsInterface && svcType.IsGenericType &&
            svcType.GetGenericTypeDefinition() == jobHandlerType)
        {
            return svcType.GetGenericArguments().First();
        }

        return svcType.GetInterfaces()
            .Select(x => GetImplementedType(x, jobHandlerType))
            .FirstOrDefault(x => x != null);
    }


    public void UseStorage<T>(Action<T>? storage = null) where T : StorageSetup, new()
    {
        var storageSetup = new T();
        storage?.Invoke(storageSetup);
        _storageSetup = storageSetup;
    }
}

public static class WindowsExtensions
{
    /// <summary>
    /// configure the default window, to limit processing of a ALL jobs
    /// </summary>
    /// <param name="max"></param>
    /// <param name="sizeInSeconds"></param>
    public static IDictionary<string, RateWindow> ConfigureGlobal(this IDictionary<string, RateWindow> windows, int max,
        int sizeInSeconds)
    {
        return windows.Configure(LatersConstants.GlobalTumbler, max, sizeInSeconds);
    }

    /// <summary>
    /// configure a window, to limit processing of a set of jobs
    /// </summary>
    /// <param name="max">max number of jobs in the window</param>
    /// <param name="sizeInSeconds">how large is the window, note the larger the window the more ram will be used.</param>
    /// <typeparam name="T">the type will be converted into the name of the window</typeparam>
    public static IDictionary<string, RateWindow> Configure<T>(this IDictionary<string, RateWindow> windows, int max,
        int sizeInSeconds)
    {
        return windows.Configure(typeof(T).FullName, max, sizeInSeconds);
    }


    /// <summary>
    /// configure a window, to limit processing of a set of jobs
    /// </summary>
    /// <param name="windowName">name of the window</param>
    /// <param name="max">max number of jobs in the window</param>
    /// <param name="sizeInSeconds">how large is the window, note the larger the window the more ram will be used.</param>
    public static IDictionary<string, RateWindow> Configure(this IDictionary<string, RateWindow> windows,
        string windowName, int max, int sizeInSeconds)
    {
        var exists = windows.TryGetValue(windowName, out var window);
        if (!exists)
        {
            window = new RateWindow();
            windows.Add(windowName, window);
        }

        window.Max = max;
        window.SizeInSeconds = sizeInSeconds;
        return windows;
    }
}

public class Windows
{
    readonly IDictionary<string, RateWindow> _windows;

    protected internal Windows(IDictionary<string, RateWindow> windows)
    {
        _windows = windows;
    }

    /// <summary>
    /// configure the default window, to limit processing of a ALL jobs
    /// </summary>
    /// <param name="max"></param>
    /// <param name="sizeInSeconds"></param>
    public virtual Windows ConfigureGlobal(int max, int sizeInSeconds)
    {
        return Configure(LatersConstants.GlobalTumbler, max, sizeInSeconds);
    }

    /// <summary>
    /// configure a window, to limit processing of a set of jobs
    /// </summary>
    /// <param name="max">max number of jobs in the window</param>
    /// <param name="sizeInSeconds">how large is the window, note the larger the window the more ram will be used.</param>
    /// <typeparam name="T">the type will be converted into the name of the window</typeparam>
    public virtual Windows Configure<T>(int max, int sizeInSeconds)
    {
        return Configure(typeof(T).FullName, max, sizeInSeconds);
    }


    /// <summary>
    /// configure a window, to limit processing of a set of jobs
    /// </summary>
    /// <param name="windowName">name of the window</param>
    /// <param name="max">max number of jobs in the window</param>
    /// <param name="sizeInSeconds">how large is the window, note the larger the window the more ram will be used.</param>
    public virtual Windows Configure(string windowName, int max, int sizeInSeconds)
    {
        var exists = _windows.TryGetValue(windowName, out var window);
        if (!exists)
        {
            window = new RateWindow();
            _windows.Add(windowName, window);
        }

        window.Max = max;
        window.SizeInSeconds = sizeInSeconds;
        return this;
    }
}

public static class SetupExtensions
{
    static Action<HostBuilderContext, Setup> ToHostBuilderConfig(
        this Action<WebHostBuilderContext, Setup> configure)
    {
        return (context, setup) =>
        {
            WebHostBuilderContext webContext = new WebHostBuilderContext()
            {
                Configuration = context.Configuration,
                HostingEnvironment = (IWebHostEnvironment)context.HostingEnvironment
            };
            configure.Invoke(webContext, setup);
        };
    }

    public static IWebHostBuilder ConfigureLaters(
        this IWebHostBuilder builder,
        Action<WebHostBuilderContext, Setup> configure)
    {
        return builder.ConfigureLaters("Laters", configure);
    }

    public static IHostBuilder ConfigureLaters(
        this IHostBuilder builder, 
        Action<HostBuilderContext, Setup> configure)
    {
        return builder.ConfigureLaters("Laters", configure);
    }


    public static IWebHostBuilder ConfigureLaters(
        this IWebHostBuilder builder, 
        string configEntry,
        Action<WebHostBuilderContext, Setup> configure)
    {
        //LatersConfiguration
        builder.ConfigureServices((context, collection) =>
        {
            Setup(context.Configuration, collection, configEntry, setup => configure?.Invoke(context, setup));
        });

        return builder;
    }
    
    
    public static IHostBuilder ConfigureLaters(
        this IHostBuilder builder, 
        string configEntry, 
        Action<HostBuilderContext, Setup> configure)
    {
        //LatersConfiguration
        builder.ConfigureServices((context, collection) =>
        {
            Setup(context.Configuration, collection, configEntry, setup => configure?.Invoke(context, setup));
        });

        return builder;
    }
    
    static void Setup(
        IConfiguration configuration, 
        IServiceCollection collection, 
        string configEntry, 
        Action<Setup> configure)
    {
        //config
        var latersConfigurationSection = configuration.GetSection(configEntry);
        var latersConfiguration = latersConfigurationSection.Get<LatersConfiguration>() ?? new LatersConfiguration();

        //ensure we have the global added in oneway or another.
        latersConfiguration.Windows.TryAdd(LatersConstants.GlobalTumbler, new RateWindow()
        {
            Max = 1_000_000, //should be high enough not to be hit (should be overridden)
            SizeInSeconds = 1
        });

        //setup the configuration before updating the IoC
        var setup = new Setup();
        setup.Configuration = latersConfiguration;
        setup.ConfigurationSection = latersConfigurationSection;
        
        //this was ANNOYING
        configure?.Invoke(setup);
        
        //apply the changes to the IoC
        setup.Apply(collection);

        //apply all other defaults to the IoC
        
        //infra
        collection.TryAddSingleton<Telemetry>();
        collection.TryAddScoped<TelemetryContext>();
        collection.TryAddSingleton<LatersMetrics>();
        collection.TryAddSingleton(latersConfiguration);
        
        //api
        collection.TryAddScoped<IAdvancedSchedule, DefaultSchedule>();
        collection.TryAddScoped<ISchedule>(provider => provider.GetRequiredService<IAdvancedSchedule>());
        collection.TryAddScoped<IScheduleCron>(provider => provider.GetRequiredService<IAdvancedSchedule>());

        //server side
        collection.TryAddSingleton<DefaultTumbler>();
        collection.TryAddSingleton<WorkerEngine>();
        collection.TryAddSingleton<JobWorkerQueue>();
        collection.TryAddSingleton<LeaderContext>();
        collection.TryAddSingleton<LeaderElectionService>();
        collection.AddHostedService<DefaultHostedService>();
        collection.TryAddTransient<WebWorker>();
        
        collection.AddHttpClient<WorkerClient>().ConfigurePrimaryHttpMessageHandler(provider =>
        {
            var handler = new HttpClientHandler();

            if (latersConfiguration.AllowPrivateCert)
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }

            handler.MaxConnectionsPerServer = latersConfiguration.NumberOfProcessingThreads;
            return handler;
        });

        collection.TryAddSingleton<IProcessJobMiddleware, ProcessJobMiddleware>();
        collection.TryAddSingleton<JobDelegates>(svc => new JobDelegates(collection));
    }

}