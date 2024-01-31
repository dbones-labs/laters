namespace Laters.Tests.Infrastructure;

using Marten;

public class ResetDataService : IHostedService
{
    readonly IDocumentStore _documentStore;

    public ResetDataService(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _documentStore
            .Advanced
            .Clean
            .DeleteAllDocumentsAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}