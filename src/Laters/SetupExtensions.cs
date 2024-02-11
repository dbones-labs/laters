namespace Laters;

using System.Reflection;
using Background;
using ClientProcessing;
using ClientProcessing.Middleware;
using Configuration;
using Default;
using Exceptions;
using Infrastucture.Telemetry;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mnimal;
using ServerProcessing;
using ServerProcessing.Engine;
using ServerProcessing.Windows;

public abstract class StorageSetup
{
    protected internal abstract void Apply(IServiceCollection serviceCollection);
}

public class Setup
{
    List<Type> _jobHandlerTypes = new();
    StorageSetup _storageSetup;

    /// <summary>
    /// update the ioc config
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <exception cref="MissingStorageConfigurationException"></exception>
    internal void Apply(IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        
        if (_storageSetup == null) throw new MissingStorageConfigurationException();
        _storageSetup.Apply(serviceCollection);

        var jobHandlerType = typeof(IJobHandler<>);
        foreach (var type in _jobHandlerTypes)
        {
            var handlerInterfaceType = jobHandlerType.MakeGenericType(GetImplementedType(type, jobHandlerType));
            serviceCollection.AddScoped(handlerInterfaceType,type);
        }
    }


    public void AddJobHandler<T>()
    {
        AddJobHandler(typeof(T));
    }
    
    public void AddJobHandler(Type handlerType)
    {
        var jobHandlerType = typeof(IJobHandler<>);
        var handlesJobType = GetImplementedType(handlerType, jobHandlerType);

        if (handlesJobType is null)
        {
            throw new NotSupportedException($"{handlerType} does not implement {jobHandlerType.Name}");
        }
        
        _jobHandlerTypes.Add(handlerType);
    }
    
    /// <summary>
    /// scan an assembly for &lt;see cref="IJobHandler{T}"/&gt; and wire them up, ready for use
    /// </summary>
    /// <typeparam name="T">a type which is within the target assembly</typeparam>
    public void ScanForJobHandlers<T>()
    {
        ScanForJobHandlers(typeof(T).Assembly);
    }

    /// <summary>
    /// scan an assembly for <see cref="IJobHandler{T}"/> and wire them up, ready for use
    /// </summary>
    /// <param name="fromHere">the target assembly</param>
    public void ScanForJobHandlers(Assembly? fromHere = null)
    {
        var jobHandlerType = typeof(IJobHandler<>);

        //default to the running project
        fromHere ??= Assembly.GetCallingAssembly(); 
        
        var types = fromHere.GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract)
            .Where(ShouldIncludeJobHandler)
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

    /// <summary>
    /// configure the rate limiting windows
    /// </summary>
    public Windows Windows { get; set; }
    
    /// <summary>
    /// this is the laters configuration, which you can configure directly or via this class
    /// </summary>
    public LatersConfiguration Configuration { get; set; }
    
    /// <summary>
    /// this is the raw configuration section, please leave this alone
    /// </summary>
    public IConfigurationSection ConfigurationSection { get; set; }
    
    /// <summary>
    /// this is where we can setup the middleware processing of any job
    /// </summary>
    public ClientActions ClientActions { get; set; }
    
    /// <summary>
    /// mainly used the find if the type implements <see cref="jobHandlerType"/> and what the type it is against.
    /// </summary>
    /// <param name="svcType">the type we are inspecting to see if it implements the desired type</param>
    /// <param name="jobHandlerType">the type we are looking for</param>
    /// <returns>the generic type it implements</returns>
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

    private bool ShouldIncludeJobHandler(Type type)
    {
        return !type.GetCustomAttributes(typeof(IgnoreAttribute), false).Any();
    }

    public void UseStorage<T>(Action<T>? storage = null) where T : StorageSetup, new()
    {
        var storageSetup = new T();
        storage?.Invoke(storageSetup);
        _storageSetup = storageSetup;
    }
}

public class MissingStorageConfigurationException : LatersException
{
    public MissingStorageConfigurationException() : base("no storage has been setup")
    {
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
        //----
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
        
        //apply the config override from the application
        //apply the changes to the IoC
        configure?.Invoke(setup);
        
        setup.Apply(collection);

        //------
        //apply all other defaults to the IoC
        //infra
        collection.TryAddSingleton<Telemetry>();
        collection.TryAddScoped<TelemetryContext>();
        collection.TryAddSingleton<LatersMetrics>();
        collection.TryAddSingleton(latersConfiguration);
        collection.TryAddSingleton<ICrontab, DefaultCrontab>();
        
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
        
        
        //client side
        collection.TryAddSingleton<ClientActions>();

        
        collection.TryAddSingleton<MinimalLambdaHandlerRegistry>();
        collection.TryAddSingleton<MinimalMapper>();
        collection.AddScoped<MinimalDelegator>();
        
        collection.TryAddSingleton(typeof(IProcessJobMiddleware<>), typeof(ProcessJobMiddleware<>));
        collection.TryAddSingleton<JobDelegates>(svc => new JobDelegates(collection));
        
        collection.TryAddSingleton(services =>
        {
            var factory = new MiddlewareDelegateFactory();
            factory.RegisterMiddlewareForAllHandlers(collection, services.GetRequiredService<MinimalLambdaHandlerRegistry>());
            return factory;
        });
        
        //out of the box middleware
        collection.TryAddScoped(typeof(FailureAction<>));
        collection.TryAddScoped(typeof(LoadJobIntoContextAction<>));
        collection.TryAddScoped(typeof(QueueNextAction<>));
        collection.TryAddScoped(typeof(HandlerAction<>));
        collection.TryAddScoped(typeof(MinimalAction<>));
    }

}