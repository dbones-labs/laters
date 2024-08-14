namespace Laters.ClientProcessing.Middleware;

using System.Diagnostics;
using Dbones.Pipes;
using Laters.Infrastructure.Telemetry;

/// <summary>
/// apply a logging scope around processing a job
/// </summary>
/// <typeparam name="T">the job type</typeparam>
public class LoggingAction<T> : IProcessAction<T>
{
    readonly ILogger<LoggingAction<T>> _logger;

    /// <summary>
    /// creates a new instance of <see cref="LoggingAction{T}"/>
    /// </summary>
    /// <param name="logger">logger</param>
    public LoggingAction(ILogger<LoggingAction<T>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task Execute(JobContext<T> context, Next<JobContext<T>> next)
    {
        var traceId = Activity.Current?.Id;

        var candidate = context.ServerRequested;
        using var __ = _logger.BeginScope(new Dictionary<string, string>
        {
            { Telemetry.LeaderId, candidate.LeaderId },
            { Telemetry.Action, "ProcessingJob" },
            { Telemetry.JobId, candidate.Id },
            { Telemetry.JobType, candidate.JobType },
            { Telemetry.Window, candidate.Window },
            { Telemetry.TraceId, traceId ?? "" }
        });

        _logger.LogInformation("processing job {jobId}", candidate.Id);

        try
        {
            //executing the cancellation check here so we can keep all the logging information on cancellation.
            context.CancellationToken.ThrowIfCancellationRequested();
            return next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "error processing job {jobId}", candidate.Id);
            throw;
        }
    }

}

