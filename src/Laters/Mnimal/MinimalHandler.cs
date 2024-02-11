namespace Laters.Mnimal;

public delegate Task MinimalHandler<T>(IServiceProvider scope, T instnace);