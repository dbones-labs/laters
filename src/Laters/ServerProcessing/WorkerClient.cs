namespace Laters.ServerProcessing;

using System.Diagnostics;
using System.Text.Json;
using ClientProcessing;
using Configuration;
using Infrastructure.Telemetry;

/// <summary>
/// used to call the workers with a job to process
/// </summary>
public class WorkerClient : IWorkerClient
{
    readonly HttpClient _httpClient;
    readonly LatersConfiguration _configuration;
    readonly ILogger<WorkerClient> _logger;

    public WorkerClient(
        HttpClient httpClient,
        LatersConfiguration configuration,
        ILogger<WorkerClient> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;

        var ep = _configuration.WorkerEndpoint ?? throw new NoWorkerEndpointException();
        _httpClient.BaseAddress = new Uri(ep);
    }

    public async Task DelegateJob(ProcessJob processJob, CancellationToken cancellationToken = default)
    {
        try
        {
            var jsonPayload = JsonSerializer.Serialize(processJob);
            var content = new JsonContent(jsonPayload);

            var response = await _httpClient.PostAsync($"laters/process-job", content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            //TODO, act on different error messgaes
            //404 forget, the job was processed
            //all other messages
            _logger.LogError(e,e.Message);
        }
    }
}