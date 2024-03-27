namespace Laters.Infrastructure.Telemetry;

/// <summary>
/// telemetry names
/// </summary>
public static class Telemetry
{
    /// <summary>
    /// the name of laters telemetry
    /// </summary>
    public static string Name = "laters.opentelemetry";

    /// <summary>
    /// when a job is enqueued
    /// </summary>
    public static string Enqueue = "laters.jobs.enqueue";

    /// <summary>
    /// when a job is processed
    /// </summary>
    public static string Process = "laters.jobs.process";

    /// <summary>
    /// when a job is processed with errors
    /// </summary>
    public static string ProcessErrors = "laters.jobs.process.errors";

    /// <summary>
    /// number of jobs that are ready to be processed (total/ guage)
    /// </summary>
    public static string Ready = "laters.jobs.ready";

    /// <summary>
    /// number of jobs that are scheduled to be processed (total / guage)
    /// </summary>
    public static string Scheduled = "laters.jobs.scheduled";

    /// <summary>
    /// number of jobs that are deadlettered (total / guage)
    /// </summary>
    public static string Deadletter = "laters.jobs.deadletter";
    
    /// <summary>
    /// number of cron jobs that are scheduled (total / guage)
    /// </summary>?
    public static string CronScheduled = "laters.cron_jobs.scheduled";

        /// <summary>
    /// time to process the message
    /// </summary>
    public static string ProcessTime = "laters.process_time";

    /// <summary>
    /// the type of the job (label)
    /// </summary>
    public static string JobType = "laters.job_type";

    /// <summary>
    /// the window in which the job was processed in (label)
    /// </summary>
    public static string Window = "laters.window";

    /// <summary>
    /// this is the key for open telemetry id, in the headers
    /// </summary>
    public static string OpenTelemetry = "laters.open_telemetry"; 
}
