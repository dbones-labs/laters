namespace Laters.ClientProcessing.Middleware;

using Pipes;

public interface IProcessAction<T> : IAction<JobContext<T>> {}