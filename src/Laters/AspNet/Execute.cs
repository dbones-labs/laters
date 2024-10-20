namespace Laters.AspNet;

using ClientProcessing;

/// <summary>
/// this is used to cache the <see cref="IProcessJobMiddleware{T}.Execute"/> method
/// so we can just call it with the scope and id
/// </summary>
/// <param name="scope">the scope to resolve the middleware from</param>
/// <param name="jobRequest">the job request to pass to the middleware</param>
/// <param name="cancellationToken">the cancellation token to pass to the middleware</param>
public delegate Task Execute(IServiceProvider scope, ProcessJob jobRequest, CancellationToken cancellationToken);