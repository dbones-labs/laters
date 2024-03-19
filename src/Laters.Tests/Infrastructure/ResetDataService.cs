namespace Laters.Tests.Infrastructure;

using Marten;

public class ResetDataService : IHostedService
{
    readonly IDocumentStore _documentStore;
    readonly ILogger<ResetDataService> _logger;

    public ResetDataService(
        IDocumentStore documentStore, 
        ILogger<ResetDataService> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{action}: {service}", nameof(StartAsync), nameof(ResetDataService));
        await _documentStore
            .Advanced
            .Clean
            .DeleteAllDocumentsAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{action}: {service}", nameof(StopAsync), nameof(ResetDataService));
        return Task.CompletedTask;
    }
}