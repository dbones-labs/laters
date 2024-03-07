namespace Laters.Minimal.Host;

using Laters.ClientProcessing;
using Laters.Minimal;

public static class HostExtensions
{
    public static void MapHandler<TMessage>(this IHost host, Delegate minimalHandle)
    {
        var mapper = host.Services.GetRequiredService<MinimalMapper>();
        mapper.Map<JobContext<TMessage>>(minimalHandle);
    }
}