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
    readonly Telemetry _telemetry;
    readonly LatersMetrics _metrics;
    readonly LatersConfiguration _configuration;
    readonly ILogger<WorkerClient> _logger;

    public WorkerClient(
        HttpClient httpClient,
        Telemetry telemetry, 
        LatersMetrics metrics,
        LatersConfiguration configuration,
        ILogger<WorkerClient> logger)
    {
        _telemetry = telemetry;
        _metrics = metrics;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;

        var ep = _configuration.WorkerEndpoint ?? throw new NoWorkerEndpointException();
        _httpClient.BaseAddress = new Uri(ep);
    }

    public async Task DelegateJob(ProcessJob processJob)
    {
        var parentId = string.Empty;
        var parent = Activity.Current;

        if (parent != null && !string.IsNullOrEmpty(parent.Id) && parent.IdFormat == ActivityIdFormat.W3C)
        {
            parentId = parent.Id;
        }

        var name = "laters.deletate-job";
        var activity = string.IsNullOrWhiteSpace(parentId)
            ? _telemetry.ActivitySource.StartActivity(name, ActivityKind.Producer)
            : _telemetry.ActivitySource.StartActivity(name, ActivityKind.Producer, parentId);

        activity?.AddTag("adapter", "laters");
        activity?.SetTag("laters.job-type", processJob.JobType);
        activity?.AddTag("laters.job-id", processJob.Id); 
        
        using (activity)
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
                _logger.LogError(e,e.Message);
            }
        }

        // Count the metric
        // var metric = _meter.CreateCounter("laters.job-type");
        _metrics.JobTypeCounter.Add(1, new KeyValuePair<string, object?>("type", processJob.JobType));
    }
}