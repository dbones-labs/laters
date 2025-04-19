namespace Laters.ServerProcessing.Workers;

using Laters.ClientProcessing;

/// <summary>
/// Dispatches jobs to the worker.
/// </summary>
public interface IWorkerClient
{
    /// <summary>
    /// This will send the job to the worker (default is via a load balancer) to be processed
    /// </summary>
    /// <param name="processJob">The Job to process</param>
    /// <param name="cancellationToken">the cancellation token</param>
    Task DelegateJob(ProcessJob processJob, CancellationToken cancellationToken = default);
}