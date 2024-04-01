namespace Laters.AspNet;

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClientProcessing;
using ClientProcessing.Middleware;
using Models;


/// <summary>
/// this is the middleware which will listen for jobs to process
/// </summary>
public class ClientMiddleware
{
    readonly RequestDelegate _next;
    readonly IServiceProvider _serviceProvider;
    readonly MiddlewareDelegateFactory _middlewareDelegateFactory;
    readonly ILogger<ClientMiddleware> _logger;
    readonly Func<IServiceProvider, Job, Task> _execute;

    /// <summary>
    /// creates a new instance of <see cref="ClientMiddleware"/>
    /// </summary>
    /// <param name="next">the next action</param>
    /// <param name="serviceProvider">the ioc</param>
    /// <param name="middlewareDelegateFactory">factory with pipelines to handle all jobs</param>
    /// <param name="logger">logger</param>
    public ClientMiddleware(
        RequestDelegate next, 
        IServiceProvider serviceProvider, 
        MiddlewareDelegateFactory middlewareDelegateFactory,
        ILogger<ClientMiddleware> logger)
    {
        _next = next;
        _serviceProvider = serviceProvider;
        _middlewareDelegateFactory = middlewareDelegateFactory;
        _logger = logger;
    }

    /// <summary>
    /// invoke the middleware
    /// </summary>
    /// <param name="context">the current http context</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var httpMethod = context.Request.Method;
        var path = context.Request.Path.Value;

        var prefix = "laters/process-job";

        if (httpMethod != "POST" || !Regex.IsMatch(path, $"^/?{Regex.Escape(prefix)}/?$", RegexOptions.IgnoreCase))
        {
            await _next(context);
            return;
        }
        
        //lets get the process job object
        ProcessJob? processJob;
        try
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var bodyContent = await reader.ReadToEndAsync();
            //TODO: workaround, fix this
            bodyContent = bodyContent
                .Substring(1, bodyContent.Length - 2)
                .Replace("\\u0022", "\"");
        
            processJob = JsonSerializer.Deserialize<ProcessJob>(bodyContent);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            context.Response.StatusCode = 412;
            await context.Response.WriteAsync("Body did not contain a valid Process Job");
            return;
        }

        try
        {
            //_leaderInformation.Id = processJob.LeaderId;
            var execute = _middlewareDelegateFactory.GetExecute(processJob.JobType);
            await execute(_serviceProvider, processJob);
        }
        catch (JobNotFoundException exception)
        {
            _logger.LogError(exception, exception.Message);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Job does not exist");
        }
    }
}