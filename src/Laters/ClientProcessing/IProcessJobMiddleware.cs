namespace Laters.ClientProcessing;

public interface IProcessJobMiddleware<T> 
{
    public Task Execute(IServiceProvider scope, JobContext<T> context);
}