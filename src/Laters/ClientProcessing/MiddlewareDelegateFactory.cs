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

        //check interface higherarchy
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
    
    Execute CreateExecuteDelegate(Type jobType)
    {
        var processJobMiddlewareType = typeof(IProcessJobMiddleware<>).MakeGenericType(jobType);
        var executeMethod = processJobMiddlewareType.GetMethod("Execute", BindingFlags.Instance | BindingFlags.Public);

        var jobContextType = typeof(JobContext<>).MakeGenericType(jobType);
        var jobContextCtor = jobContextType.GetConstructor(Type.EmptyTypes);
        var jobProperty = jobContextType.GetProperty("JobId");
        
        return (scope, job) =>
        {
            var ctxInstance = jobContextCtor.Invoke(null);
            jobProperty.SetValue(ctxInstance, job);

            var middleware = scope.GetRequiredService(processJobMiddlewareType);
            return (Task)executeMethod.Invoke(middleware, new object[] { scope, ctxInstance });
        };
    }

    Execute CreateExecuteDelegate2(Type jobType)
    {
        var processJobMiddlewareType = typeof(IProcessJobMiddleware<>).MakeGenericType(jobType);
        var executeMethod = processJobMiddlewareType.GetMethod("Execute", BindingFlags.Instance | BindingFlags.Public);

        var jobContextType = typeof(JobContext<>).MakeGenericType(jobType);
        var jobContextCtor = jobContextType.GetConstructor(Type.EmptyTypes);
        var jobProperty = jobContextType.GetProperty("JobId");

        var scopeParam = Expression.Parameter(typeof(IServiceProvider), "scope");
        var jobParam = Expression.Parameter(typeof(string), "jobId");

        var ctxInstanceExpr = Expression.New(jobContextCtor);
        var setJobPropertyExpr = Expression.Call(ctxInstanceExpr, jobProperty.SetMethod, jobParam);

        var middlewareVar = Expression.Variable(processJobMiddlewareType, "middleware");
        var getMiddlewareExpr = Expression.Assign(middlewareVar, Expression.Call(scopeParam, typeof(IServiceProvider).GetMethod("GetRequiredService").MakeGenericMethod(processJobMiddlewareType)));

        var executeMethodCall = Expression.Call(middlewareVar, executeMethod, scopeParam, ctxInstanceExpr);
    
        var lambdaExpr = Expression.Lambda<Execute>(
            Expression.Block(
                new[] { middlewareVar },
                ctxInstanceExpr,
                setJobPropertyExpr,
                getMiddlewareExpr,
                executeMethodCall
            ),
            scopeParam,
            jobParam
        );

        return lambdaExpr.Compile();
    }
}