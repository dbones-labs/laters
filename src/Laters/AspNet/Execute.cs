namespace Laters.AspNet;

using ClientProcessing;

/// <summary>
/// this is used to cache the <see cref="IProcessJobMiddleware{T}.Execute"/> method
/// so we can just call it with the scope and id
/// </summary>
public delegate Task Execute(IServiceProvider scope, ProcessJob jobRequest);