namespace Laters;

using Background;
using ClientProcessing;
using ClientProcessing.Middleware;
using Configuration;
using Default;
using Infrastucture;
using Infrastucture.Cron;
using Infrastucture.Telemetry;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minimal;
using ServerProcessing;
using ServerProcessing.Engine;
using ServerProcessing.Windows;

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
        collection.TryAddSingleton<JobWorkerQueue>();
        collection.TryAddSingleton<LeaderContext>();
        collection.TryAddSingleton<LeaderElectionService>();
        collection.AddHostedService<DefaultHostedService>();
        
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
        collection.AddHostedService<GlobalCronSetup>();
        collection.AddTransient<GlobalScheduleCronProxy>();
        
        collection.TryAddScoped<LeaderInformation>();
        
        collection.TryAddSingleton<MinimalLambdaHandlerRegistry>();
        collection.TryAddSingleton<MinimalMapper>();
        collection.AddScoped<MinimalDelegator>();
        
        collection.TryAddSingleton<ClientActions>();
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
        collection.TryAddScoped(typeof(PersistenceAction<>));
        collection.TryAddScoped(typeof(CronAction<>));
        collection.TryAddScoped(typeof(HandlerAction<>));
        collection.TryAddScoped(typeof(MinimalAction<>));
    }
}