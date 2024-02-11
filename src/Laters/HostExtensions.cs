namespace Laters;

using ClientProcessing;
using Mnimal;

public static class HostExtensions
{
    public static void MapHandler<TMessage>(this IHost host, Delegate minimalHandle)
    {
        var mapper = host.Services.GetRequiredService<MinimalMapper>();
        mapper.Map<JobContext<TMessage>>(minimalHandle);
    }
    
    public static void MapHandler<TMessage>(this IApplicationBuilder host, Delegate minimalHandle)
    {
        var mapper = host.ApplicationServices.GetRequiredService<MinimalMapper>();
        mapper.Map<JobContext<TMessage>>(minimalHandle);
    }
}