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

    /// <summary>
    /// create a new instance of <see cref="WorkerClient"/>
    /// </summary>
    /// <param name="httpClient">the configured http client</param>
    /// <param name="configuration">laters config</param>
    /// <param name="logger">logger</param>
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

    /// <summary>
    /// send the job to the worker (load balancer) to be process
    /// </summary>
    /// <param name="processJob">job the process</param>
    /// <param name="cancellationToken">cancellation token</param>
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
            //TODO, act on different error messages
            //404 forget, the job was processed
            //all other messages
            _logger.LogError(e,e.Message);
        }
    }
}