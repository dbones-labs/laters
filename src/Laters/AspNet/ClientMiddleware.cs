namespace Laters.AspNet;

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public class ClientMiddleware : IMiddleware
{
    readonly IServiceProvider _serviceProvider;
    readonly IProcessJobMiddleware _middleware;

    public ClientMiddleware(IServiceProvider serviceProvider, IProcessJobMiddleware middleware)
    {
        _serviceProvider = serviceProvider;
        _middleware = middleware;
    }
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var httpMethod = context.Request.Method;
        var path = context.Request.Path.Value;

        var prefix = "laters/process-job";

        if (httpMethod != "GET" || !Regex.IsMatch(path, $"^/?{Regex.Escape(prefix)}/?$", RegexOptions.IgnoreCase))
        {
            await next(context);
            return;
        }
        
        //lets get the process job object
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var bodyContent = await reader.ReadToEndAsync();
        
        var processJob = JsonSerializer.Deserialize<ProcessJob>(bodyContent);

        if (processJob == null)
        {
            context.Response.StatusCode = 412;
            await context.Response.WriteAsync("Body did not contain a valid Process Job");
            return;
        }
        
        //load from the database
        var session = _serviceProvider.GetRequiredService<ISession>();
        var job = await session.GetById<Job>(processJob.Id);

        if (job == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Job does not exist");
            return;
        }

        await _middleware.Execute(_serviceProvider, job);
    }
}

public static class ApplicationBuilderExtensions
{
    public static void UseLaters(this IApplicationBuilder app)
    {
        app.UseMiddleware<ClientMiddleware>();
    }
}
