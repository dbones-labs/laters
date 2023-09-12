namespace Laters;

using Microsoft.Extensions.DependencyInjection.Extensions;

public static class SetupExtensions 
{

    public static IWebHostBuilder ConfigureLaters(this IWebHostBuilder builder)
    {
        builder.ConfigureServices((context, collection)  =>
        {
            collection.TryAddSingleton<Telemetry>();
            collection.TryAddScoped<TelemetryContext>();
            collection.TryAddSingleton<LatersMetrics>();

            collection.AddHttpClient<WorkerClient>().ConfigurePrimaryHttpMessageHandler(provider =>
            {
                var configuration = provider.GetRequiredService<LatersConfiguration>();
                var handler = new HttpClientHandler();
                
                if (configuration.AllowPrivateCert)
                {
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                }

                handler.MaxConnectionsPerServer = configuration.NumberOfProcessingThreads;
                return handler;
            });
            
            collection.TryAddSingleton<IProcessJobMiddleware, ProcessJobMiddleware>();
            
            collection.TryAddSingleton<JobDelegates>(svc => new JobDelegates(collection));
            
            //collection.Select(x=> x.ServiceType)
        });

        return builder;
    }

}