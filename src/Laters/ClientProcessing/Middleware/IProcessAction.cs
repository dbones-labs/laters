namespace Laters.ClientProcessing.Middleware;

using Dbones.Pipes;


/// <summary>
/// the process interface which will be excuted against the <see cref="JobContext{T}"/>
/// </summary>
/// <typeparam name="T">a job type</typeparam>
/// <remarks><see cref="ClientActions"/> is where you can override and provide your own</remarks>
public interface IProcessAction<T> : IAction<JobContext<T>> {}