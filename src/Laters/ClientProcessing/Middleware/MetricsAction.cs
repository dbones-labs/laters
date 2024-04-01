namespace Laters.ClientProcessing.Middleware;

using System.Diagnostics;
using Dbones.Pipes;
using Laters.Infrastructure.Telemetry;

/// <summary>
/// apply metrics to the job processing
/// </summary>
/// <typeparam name="T">the job type</typeparam>
public class MetricsAction<T> : IProcessAction<T>
{
    readonly IMetrics _metrics;

    /// <summary>
    /// creates a new instance of <see cref="MetricsAction{T}"/>
    /// </summary>
    /// <param name="metrics">metrics wrapper</param>
    public MetricsAction(IMetrics metrics)
    {
        _metrics = metrics;
    }

    /// <inheritdoc />
    public async Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        var candidate = context.ServerRequested;
        var tagList = new TagList
        {
            { Telemetry.JobType, candidate.JobType },
            { Telemetry.Window, candidate.Window }
        };

        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
            _metrics.ProcessCounter.Add(1, tagList);
            _metrics.ProcessTime.Record(sw.Elapsed.TotalMilliseconds, tagList);
        }
        catch (Exception)
        {
            _metrics.ProcessErrorsCounter.Add(1, tagList);
            throw;
        }
    }
}