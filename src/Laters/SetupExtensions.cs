namespace Laters;

using Background;
using ClientProcessing;
using ClientProcessing.Middleware;
using Configuration;
using Default;
using Infrastructure;
using Infrastructure.Cron;
using Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minimal;
using ServerProcessing;
using ServerProcessing.Engine;
using ServerProcessing.Windows;

/// <summary>
/// Extensions to setup the Laters library.
/// </summary>
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

    /// <summary>
    /// Configure laters from the <see cref="IWebHostBuilder"/>
    /// </summary>
    /// <param name="builder">the web host</param>
    /// <param name="configure">the configuration to apply</param>
    public static IWebHostBuilder ConfigureLaters(
        this IWebHostBuilder builder,
        Action<WebHostBuilderContext, Setup> configure)
    {
        return builder.ConfigureLaters("Laters", configure);
    }

    /// <summary>
    /// Configure laters from the <see cref="IHostBuilder"/>
    /// </summary>
    /// <param name="builder">the host</param>
    /// <param name="configure">the configuration to apply</param>
    /// <returns></returns>
    public static IHostBuilder ConfigureLaters(
        this IHostBuilder builder, 
        Action<HostBuilderContext, Setup> configure)
    {
        return builder.ConfigureLaters("Laters", configure);
    }


    /// <summary>
    /// Configure laters from the <see cref="IWebHostBuilder"/>
    /// </summary>
    /// <param name="builder">the web host</param>
    /// <param name="configEntry">the name of the configuration to ready from, default is `Laters`</param>
    /// <param name="configure">configuration to apply</param>
    public static IWebHostBuilder ConfigureLaters(
        this IWebHostBuilder builder, 
        string configEntry,
        Action<WebHostBuilderContext, Setup> configure)
    {
        //LatersConfiguration
        builder.ConfigureServices((context, collection) =>
        {
            Setup(context.Configuration, collection, configEntry, setup => configure.Invoke(context, setup));
        });

        return builder;
    }
    
    /// <summary>
    /// Configure laters from the <see cref="IHostBuilder"/>
    /// </summary>
    /// <param name="builder">the web host</param>
    /// <param name="configEntry">the name of the configuration to ready from, default is `Laters`</param>
    /// <param name="configure">configuration to apply</param>
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
    
    /// <summary>
    /// Setup the laters configuration
    /// </summary>
    /// <param name="configuration">the application config, used to get the laters config options</param>
    /// <param name="collection">the ioc container collection</param>
    /// <param name="configEntry">the name of the config entry</param>
    /// <param name="configure">the configuration to apply</param>
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
        var setup = new Setup
        {
            Configuration = latersConfiguration,
            ConfigurationSection = latersConfigurationSection
        };

        //apply the config override from the application
        //apply the changes to the IoC
        configure?.Invoke(setup);

        //let's try and be helpful
        if (string.IsNullOrWhiteSpace(setup.Configuration.WorkerEndpoint))
        {
            setup.Configuration.WorkerEndpoint = configuration["ASPNETCORE_URLS"];
        }
        
        setup.Apply(collection);

        //------
        //apply all other defaults to the IoC
        //infra
        collection.TryAddSingleton<IMetrics, Metrics>();
        collection.TryAddSingleton<Traces>();
        collection.TryAddScoped<TelemetryContext>();
        collection.TryAddSingleton<StorageMetricsRunner>();
        collection.TryAddSingleton(latersConfiguration);
        collection.TryAddSingleton<ICrontab, DefaultCrontab>();
        
        //api
        collection.TryAddScoped<IAdvancedSchedule, DefaultSchedule>();
        collection.TryAddScoped<ISchedule>(provider => provider.GetRequiredService<IAdvancedSchedule>());
        collection.TryAddScoped<IScheduleCron>(provider => provider.GetRequiredService<IAdvancedSchedule>());

        //server side
        collection.TryAddSingleton<ITumbler, DefaultTumbler>();
        collection.TryAddSingleton<JobWorkerQueue>();
        collection.TryAddSingleton<LeaderContext>();
        collection.TryAddSingleton<EnsureJobInstancesForCron>();
        collection.TryAddSingleton<LeaderElectionService>();
        collection.AddHostedService<DefaultHostedService>();

        
        
        //we allow the user to choose how they want to use the distributed clients or just a in process one.
        if (latersConfiguration.UseInProcessClient)
        {
            collection.AddSingleton<IWorkerClient, InProcessClient>();
        }
        else
        {
            collection.AddHttpClient<IWorkerClient, WorkerClient>().ConfigurePrimaryHttpMessageHandler(_ =>
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
        }

        //client side
        collection.AddHostedService<GlobalCronSetup>();
        collection.AddTransient<GlobalScheduleCronProxy>();
        
        collection.TryAddScoped<LeaderInformation>();
        
        collection.TryAddSingleton<MinimalLambdaHandlerRegistry>();
        collection.TryAddSingleton<MinimalMapper>();
        collection.AddScoped<MinimalDelegator>();
        
        collection.TryAddSingleton<ClientActions>();
        collection.TryAddSingleton(typeof(IProcessJobMiddleware<>), typeof(ProcessJobMiddleware<>));
        collection.TryAddSingleton<JobDelegates>(_ => new JobDelegates(collection));
        
        collection.TryAddSingleton(services =>
        {
            var factory = new MiddlewareDelegateFactory();
            factory.RegisterMiddlewareForAllHandlers(collection, services.GetRequiredService<MinimalLambdaHandlerRegistry>());
            return factory;
        });
        
        //out of the box middleware
        collection.TryAddScoped(typeof(FailureAction<>));
        collection.TryAddScoped(typeof(PersistenceAction<>));
        collection.TryAddScoped(typeof(CronAction<>));
        collection.TryAddScoped(typeof(HandlerAction<>));
        collection.TryAddScoped(typeof(MinimalAction<>));
        collection.TryAddScoped(typeof(MetricsAction<>));
        collection.TryAddScoped(typeof(TraceAction<>));
        collection.TryAddScoped(typeof(LoggingAction<>));
    }
}