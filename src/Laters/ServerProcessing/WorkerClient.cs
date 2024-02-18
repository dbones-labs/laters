namespace Laters.ServerProcessing;

using System.Diagnostics;
using System.Text.Json;
using ClientProcessing;
using Configuration;
using Infrastucture.Telemetry;

/// <summary>
/// used to call the workers with a job to process
/// </summary>
public class WorkerClient : IWorkerClient
{
    readonly HttpClient _httpClient;
    readonly LatersMetrics _metrics;
    readonly LatersConfiguration _configuration;
    readonly ILogger<WorkerClient> _logger;

    public WorkerClient(
        HttpClient httpClient,
        LatersMetrics metrics,
        LatersConfiguration configuration,
        ILogger<WorkerClient> logger)
    {
        _metrics = metrics;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;

        var ep = _configuration.WorkerEndpoint ?? throw new NoWorkerEndpointException();
        _httpClient.BaseAddress = new Uri(ep);
    }

    public async Task DelegateJob(ProcessJob processJob)
    {
        try
        {
            var jsonPayload = JsonSerializer.Serialize(processJob);
            var content = new JsonContent(jsonPayload);

            var response = await _httpClient.PostAsync($"laters/process-job", content);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            //TODO, act on different error messgaes
            //404 forget, the job was processed
            //all other messages
            _logger.LogError(e,e.Message);
        }
        
        // Count the metric
        // var metric = _meter.CreateCounter("laters.job-type");
        _metrics.JobTypeCounter.Add(1, new KeyValuePair<string, object?>("type", processJob.JobType));
    }
}