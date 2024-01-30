namespace Laters.AspNet;

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Middleware;

public class ClientMiddleware
{
    readonly RequestDelegate _next;
    readonly IServiceProvider _serviceProvider;
    readonly MiddlewareDelegateFactory _middlewareDelegateFactory;
    readonly ILogger<ClientMiddleware> _logger;

    readonly Func<IServiceProvider, Job, Task> _execute;

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
            var execute = _middlewareDelegateFactory.GetExecute(processJob.JobType);
            await execute(_serviceProvider, processJob.Id);
        }
        catch (JobNotFoundException exception)
        {
            _logger.LogError(exception, exception.Message);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Job does not exist");
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}

public class JobTypeWithMoreThanOneHandler : LatersException
{
    public string JobType { get; }

    public JobTypeWithMoreThanOneHandler(string jobType)
        : base($"there is more than one handler for {jobType}")
    {
        JobType = jobType;
    }
}

public class NoJobTypeFoundException : LatersException
{
    public string JobType { get; }

    public NoJobTypeFoundException(string jobType) 
        : base($"Cannot find job {jobType}")
    {
        JobType = jobType;
    }
}

public delegate Task Execute(IServiceProvider scope, string jobId);