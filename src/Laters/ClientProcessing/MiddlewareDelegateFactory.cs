namespace Laters.ClientProcessing;

using System.Linq.Expressions;
using System.Reflection;
using AspNet;
using Minimal;

/// <summary>
/// this is used to store the complete collection of handler pipelines
/// </summary>
public class MiddlewareDelegateFactory
{
    Dictionary<string, Execute> _middlewareDelegates = new();

    /// <summary>
    /// gets the pipeline to execute for the given type
    /// </summary>
    /// <param name="typeName">the type which you are going to execute a pipeline against</param>
    /// <returns></returns>
    public Execute GetExecute(string typeName)
    {
        if (_middlewareDelegates.TryGetValue(typeName, out var @delegate))
        {
            return @delegate;
        }

        throw new NoJobTypeFoundException(typeName);
    }

    public void RegisterMiddlewareForAllHandlers(IServiceCollection collection, MinimalLambdaHandlerRegistry registry)
    {
        //check the ioc for types which implement the IJobHandler<>
        var requiredOpenGeneric = typeof(IJobHandler<>);
        var jobTypes = collection
            .Select(x => new
            {
                Params = GetParamsWhereImplements(x.ImplementationType, requiredOpenGeneric).Distinct()
            })
            .Where(x => x.Params.Any())
            .SelectMany(x => x.Params);

        //add minimal api types too
        var allJobTypes = jobTypes.Union(registry.Supported.SelectMany(x => x.GenericTypeArguments));

        foreach (var jobType in allJobTypes)
        {
            var name = jobType.FullName!;
            var execute = CreateExecuteDelegate(jobType);
            if (!_middlewareDelegates.TryAdd(name, execute))
            {
                throw new JobTypeWithMoreThanOneHandler(name);
            }
        }
    }

    protected IEnumerable<Type> GetParamsWhereImplements(Type? actual, Type requiredOpenGeneric)
    {
        if (actual == typeof(object) || actual is null) yield break;

        var candidates = actual
            .GetInterfaces()
            .Where(x => x.IsGenericType)
            .Where(x => x.GetGenericTypeDefinition() == requiredOpenGeneric)
            .SelectMany(x => x.GenericTypeArguments);

        foreach (var candidate in candidates)
        {
            yield return candidate;
        }

        //check interface hierarchy
        var upperCandidates = actual
            .GetInterfaces()
            .SelectMany(x => GetParamsWhereImplements(x, requiredOpenGeneric));

        foreach (var candidate in upperCandidates)
        {
            yield return candidate;
        }

        //check base types
        var parentCandidates = GetParamsWhereImplements(actual.BaseType, requiredOpenGeneric);
        foreach (var candidate in parentCandidates)
        {
            yield return candidate;
        }
    }


    /// <summary>
    /// this create the delegate which will call the pipeline
    /// </summary>
    /// <param name="jobType">the job type which the pipeline is for</param>
    /// <remarks>this uses reflection, which slower, but easier to debug.. <see cref="CreateExecuteDelegate2"/> the the lambda version</remarks>
    Execute CreateExecuteDelegate(Type jobType)
    {
        const string executeName = nameof(Execute);
        const string ServerRequestedName = nameof(JobContext<object>.ServerRequested);
        const string CancellationTokenName = nameof(JobContext<object>.CancellationToken);

        var processJobMiddlewareType = typeof(IProcessJobMiddleware<>).MakeGenericType(jobType);
        var executeMethod = processJobMiddlewareType.GetMethod(executeName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new Exception($"no {executeName} method found for {processJobMiddlewareType.FullName}");

        var jobContextType = typeof(JobContext<>).MakeGenericType(jobType);

        var jobContextCtor = jobContextType.GetConstructor(Type.EmptyTypes)
            ?? throw new Exception($"no ctor found for {jobContextType.FullName}");

        var jobProperty = jobContextType.GetProperty(ServerRequestedName)
            ?? throw new Exception($"{ServerRequestedName} property found for {jobContextType.FullName}");

        var jobCancellationToken = jobContextType.GetProperty(CancellationTokenName)
            ?? throw new Exception($"{CancellationTokenName} property found for {jobContextType.FullName}");

        //this is the function which will call the pipeline.
        return (scope, job, cancellationToken) =>
        {
            var ctxInstance = jobContextCtor.Invoke(null);
            jobProperty.SetValue(ctxInstance, job);
            jobCancellationToken.SetValue(ctxInstance, cancellationToken);

            var middleware = scope.GetRequiredService(processJobMiddlewareType);
            return (Task)executeMethod.Invoke(middleware, new object[] { scope, ctxInstance })!;
        };
    }

    /// <summary>
    /// this create the delegate which will call the pipeline
    /// </summary>
    /// <param name="jobType">the job type which the pipeline is for</param>
    /// <remarks>under construction....</remarks>
    Execute CreateExecuteDelegate2(Type jobType)
    {
        const string executeName = nameof(Execute);
        const string ServerRequestedName = nameof(JobContext<object>.ServerRequested);
        const string CancellationTokenName = nameof(JobContext<object>.CancellationToken);

        var processJobMiddlewareType = typeof(IProcessJobMiddleware<>).MakeGenericType(jobType);
        var executeMethod = processJobMiddlewareType.GetMethod(executeName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new Exception($"no {executeName} method found for {processJobMiddlewareType.FullName}");

        var jobContextType = typeof(JobContext<>).MakeGenericType(jobType);
        var jobContextCtor = jobContextType.GetConstructor(Type.EmptyTypes)
            ?? throw new Exception($"no ctor found for {jobContextType.FullName}");
        var jobProperty = jobContextType.GetProperty(ServerRequestedName)
            ?? throw new Exception($"{ServerRequestedName} property not found for {jobContextType.FullName}");
        var jobCancellationToken = jobContextType.GetProperty(CancellationTokenName)
            ?? throw new Exception($"{CancellationTokenName} property not found for {jobContextType.FullName}");

        var scopeParam = Expression.Parameter(typeof(IServiceScope), "scope");
        var jobParam = Expression.Parameter(typeof(ProcessJob), "job");
        var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var ctxInstanceVar = Expression.Variable(jobContextType, "ctxInstance");
        var middlewareVar = Expression.Variable(processJobMiddlewareType, "middleware");

        var block = Expression.Block(
            new[] { ctxInstanceVar, middlewareVar },
            
            // var ctxInstance = new JobContext<jobType>();
            Expression.Assign(ctxInstanceVar, Expression.New(jobContextCtor)),
            
            // ctxInstance.ServerRequested = (jobType)job;
            Expression.Assign(
                Expression.Property(ctxInstanceVar, jobProperty),
                Expression.Convert(jobParam, jobType)
            ),
            
            // ctxInstance.CancellationToken = cancellationToken;
            Expression.Assign(
                Expression.Property(ctxInstanceVar, jobCancellationToken),
                cancellationTokenParam
            ),
            
            // var middleware = scope.GetRequiredService<IProcessJobMiddleware<jobType>>();
            Expression.Assign(
                middlewareVar,
                Expression.Call(
                    typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), new[] { typeof(IServiceProvider), typeof(Type) })!,
                    Expression.Property(scopeParam, "ServiceProvider"),
                    Expression.Constant(processJobMiddlewareType)
                )
            ),
            
            // return (Task)middleware.Execute(scope, ctxInstance);
            Expression.Convert(
                Expression.Call(
                    Expression.Convert(middlewareVar, processJobMiddlewareType),
                    executeMethod,
                    scopeParam,
                    ctxInstanceVar
                ),
                typeof(Task)
            )
        );

        var lambda = Expression.Lambda<Execute>(block, scopeParam, jobParam, cancellationTokenParam);
        return lambda.Compile();
    }
}