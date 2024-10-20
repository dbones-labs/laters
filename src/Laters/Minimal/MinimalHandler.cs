namespace Laters.Minimal;

/// <summary>
/// the signature required to execute a minimal handler
/// </summary>
/// <typeparam name="T">the job type</typeparam>
public delegate Task MinimalHandler<T>(IServiceProvider scope, T instance);