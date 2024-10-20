namespace Laters.Minimal.Application;

using Laters.ClientProcessing;
using Laters.Minimal;

public static class ApplicationExtensions
{
    public static void MapHandler<TMessage>(this IApplicationBuilder host, Delegate minimalHandle)
    {
        var mapper = host.ApplicationServices.GetRequiredService<MinimalMapper>();
        mapper.Map<JobContext<TMessage>>(minimalHandle);
    }
}