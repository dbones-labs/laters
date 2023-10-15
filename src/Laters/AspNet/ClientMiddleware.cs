namespace Laters.AspNet;

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;

public class ClientMiddleware
{
    readonly RequestDelegate _next;
    readonly IServiceProvider _serviceProvider;
    readonly IProcessJobMiddleware _middleware;

    public ClientMiddleware(RequestDelegate next, IServiceProvider serviceProvider, IProcessJobMiddleware middleware)
    {
        _next = next;
        _serviceProvider = serviceProvider;
        _middleware = middleware;
    }

    public async Task Invoke(HttpContext context)
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
            //workaround, this fix
            bodyContent = bodyContent
                .Substring(1, bodyContent.Length - 2)
                .Replace("\\u0022", "\"");
        
            processJob = JsonSerializer.Deserialize<ProcessJob>(bodyContent);
        }
        catch (Exception exception)
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
    
    public static String Decode(string content)
    {
        String text;
        Byte[] bytes;
        using (StreamReader sr = new StreamReader(content))
        {
            text = sr.ReadToEnd();
            
            return text;
        }
    }
}

public static class ApplicationBuilderExtensions
{
    public static void UseLaters(this IApplicationBuilder app)
    {
        app.UseMiddleware<ClientMiddleware>();
    }
}


