namespace Laters.Minimal;

using System.Linq.Expressions;
using ClientProcessing;

/// <summary>
/// this is where we register a handler via the minimal api
/// </summary>
public class MinimalMapper
{
    readonly MinimalLambdaHandlerRegistry _registry;

    public MinimalMapper(MinimalLambdaHandlerRegistry registry)
    {
        _registry = registry;
    }
    
    /// <summary>
    /// register a delegate to handle a job
    /// </summary>
    /// <param name="impl">the code to run when the job is fired, all params supports injection from the container, and one should be the <see cref="JobContext{T}"/> which will be the job data</param>
    /// <typeparam name="T">the type of job this will handle</typeparam>
    /// <remarks>
    /// this is one way to support handling a job, you can create a class which implements <see cref="IJobHandler{T}"/>
    /// </remarks>
    public void Map<T>(Delegate impl)
    {
        var handler = CompileHandler<T>(impl);
        _registry.Add(handler);
    }
    
    static MinimalHandler<T> CompileHandler<T>(Delegate impl)
    {
        var scopeParameter = Expression.Parameter(typeof(IServiceProvider), "scope");
        var instanceParameter = Expression.Parameter(typeof(T), "instance");

        var methodInfo = impl.Method;
        var targetExpression = impl.Target != null ?
            Expression.Constant(impl.Target) : null;

        var parameters = new List<Expression>();

        foreach (var parameter in methodInfo.GetParameters())
        {
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                var tokenProperty = Expression.Property(instanceParameter, nameof(JobContext<object>.CancellationToken));
                parameters.Add(tokenProperty);
            }
            else if (parameter.ParameterType == typeof(T))
            {
                parameters.Add(instanceParameter);
            }
            else
            {
                var resolveParamExpr = Expression.Call(
                    typeof(ServiceProviderServiceExtensions), nameof(ServiceProviderServiceExtensions.GetRequiredService),
                    null, scopeParameter, Expression.Constant(parameter.ParameterType));

                var resolveParam = Expression.Convert(resolveParamExpr, parameter.ParameterType);

                parameters.Add(resolveParam);
            }
        }

        Expression methodCall;

        if (impl.Target != null)
        {
            methodCall = Expression.Call(
                Expression.Constant(impl.Target),
                methodInfo,
                parameters);
        }
        else
        {
            methodCall = Expression.Call(
                methodInfo,
                parameters);
        }

        var methodTaskResultType = methodInfo.ReturnType;
        if (methodTaskResultType != typeof(Task))
        {
            // If the method doesn't return Task, wrap it with Task.FromResult
            methodCall = Expression.Call(
                typeof(Task), 
                nameof(Task.FromResult), 
                new[] { methodTaskResultType }, 
                methodCall);
        }

        var body = Expression.Block(methodCall);

        var lambda = Expression.Lambda<MinimalHandler<T>>(body, scopeParameter, instanceParameter);
        return lambda.Compile();
    }
}