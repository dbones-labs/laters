namespace Laters;

using System.Linq.Expressions;
using System.Text.Json;
using Pipes;

public class JobDelegates
{
    private Dictionary<Type, Func<IServiceProvider, Job, Task>> _delegates = new();
    
    public JobDelegates(IServiceCollection collection)
    {
        var jobHandlerType = typeof(IJobHandler<>);

        var handlerTypes = collection
            .Select(x => GetImplementedType(x.ServiceType, jobHandlerType))
            .Where(x => x != null)
            .Distinct();

        foreach (var handlerType in handlerTypes)
        {
            var func = CreateExecuteJobLambda(handlerType);
            _delegates.Add(handlerType, func);
        }

    }
    
    static Type? GetImplementedType(Type svcType, Type jobHandlerType)
    {
        if (svcType.IsInterface && svcType.IsGenericType &&
            svcType.GetGenericTypeDefinition() == jobHandlerType)
        {
            return svcType.GetGenericArguments().First();
        }

        return svcType.GetInterfaces()
            .Select(x => GetImplementedType(x, jobHandlerType))
            .FirstOrDefault(x => x != null);
    }

    public async Task Execute(IServiceProvider serviceProvider, Job job)
    {
        if (job.JobType is null) throw new Exception($"no job type for {job.Id}");
        if (_delegates.TryGetValue(job.JobType, out var func))
        {
            await func(serviceProvider, job);
        }
        else
        {
            throw new Exception($"cannot find a handler for job type {job.JobType}");
        }
    }
    
    
    public static Func<IServiceProvider, Job, Task> CreateExecuteJobLambda(Type handlerType)
    {
        var scopeParam = Expression.Parameter(typeof(IServiceProvider), "scope");
        var jobParam = Expression.Parameter(typeof(Job), "job");

        var executeJobMethod = typeof(JobDelegates)
            .GetMethod("ExecuteJob")
            ?.MakeGenericMethod(handlerType);
        
        var executeJobCall = Expression.Call(
            executeJobMethod,
            scopeParam,
            jobParam
        );

        var lambda = Expression.Lambda<Func<IServiceProvider, Job, Task>>(
            executeJobCall,
            scopeParam,
            jobParam
        );

        return lambda.Compile();
    }
    
    public static async Task ExecuteJob<T>(IServiceProvider scope, Job job)
    {
        var payload = string.IsNullOrWhiteSpace(job.Payload) 
            ? default
            : JsonSerializer.Deserialize<T?>(job.Payload);
        
        var context = new JobContext<T>
        {
            Payload = payload,
            Job = job
        };
        
        var handler = scope.GetRequiredService<IJobHandler<T>>();
        await handler.Execute(context);
    }
}


public interface IProcessJobMiddleware
{
    Task Execute(IServiceProvider scope, Job context);
}

/// <summary>
/// this is to process the Job
/// </summary>
public class ProcessJobMiddleware : IProcessJobMiddleware, IMiddleware<Job>
{
    readonly Middleware<Job> _internalMiddleware;

    public ProcessJobMiddleware()
    {
        _internalMiddleware = new Middleware<Job>();
        
        //we have loaded from the dataabse
        //Otel
        //custom processing
        //Ijobhandler

    }
    
    public async Task Execute(IServiceProvider scope, Job context)
    {
        await _internalMiddleware.Execute(scope, context);
    }
}


public class ProcessContextActions
{
    public Type PrepareMessageContextForPublish { get; set; }
    public Type InvokePublisherAction { get; set; }
    public Type ApmAction { get; set; }
    //public Type LoggingAction { get; set; }
    public List<Type> CustomActions { get; set; } = new List<Type>();
}


public class InvokeJobHandlerAction : IProcessAction
{
    readonly IServiceProvider _scope;
    readonly JobDelegates _jobDelegates;

    public InvokeJobHandlerAction(IServiceProvider scope, JobDelegates jobDelegates)
    {
        _scope = scope;
        _jobDelegates = jobDelegates;
    }
    
    public async Task Execute(Job context, Next<Job> next)
    {
        await _jobDelegates.Execute(_scope, context);
    }
}