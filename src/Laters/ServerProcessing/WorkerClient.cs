﻿namespace Laters;

using System.Diagnostics;
using System.Text;
using System.Text.Json;

/// <summary>
/// todo register
/// </summary>
public class WorkerClient : IWorkerClient
{
    private readonly HttpClient _httpClient;
    private readonly Telemetry _telemetry;
    private readonly LatersMetrics _metrics;
    private readonly LatersConfiguration _configuration;


    public WorkerClient(
        HttpClient httpClient,
        Telemetry telemetry, 
        LatersMetrics metrics,
        LatersConfiguration configuration)
    {
       
        _telemetry = telemetry;
        _metrics = metrics;
        _configuration = configuration;
        _httpClient = httpClient;

        var ep = _configuration.WorkerEndpoint ?? throw new Exception("please provide a worker-endpoint");
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
            var jsonPayload = JsonSerializer.Serialize(processJob);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/laters/process-job", content);

            response.EnsureSuccessStatusCode();
        }

        // Count the metric
        //var metric = _meter.CreateCounter("laters.job-type");
        _metrics.JobTypeCounter.Add(1, new KeyValuePair<string, object?>("type", processJob.JobType));
    }
}