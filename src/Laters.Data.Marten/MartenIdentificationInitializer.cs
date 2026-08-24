using System.Diagnostics.CodeAnalysis;
using global::Marten;
using Microsoft.Extensions.Hosting;

namespace Laters.Data.Marten;

internal sealed class MartenIdentificationInitializer : IHostedService
{
    private readonly IDocumentStore _store;

    public MartenIdentificationInitializer(IDocumentStore store)
    {
        _store = store;
    }

    [RequiresUnreferencedCode("Registers configured Marten identification providers through reflection.")]
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _store.RegisterIdentifications();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
