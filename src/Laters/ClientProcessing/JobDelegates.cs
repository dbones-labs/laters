namespace Laters.ClientProcessing;

using System.Linq.Expressions;
using System.Text.Json;
using Models;

[Obsolete("not in use", false)]
public class JobDelegates
{
    Dictionary<string, Func<IServiceProvider, Job, Task>> _delegates = new();
    
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
            _delegates.Add(handlerType.FullName, func);
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