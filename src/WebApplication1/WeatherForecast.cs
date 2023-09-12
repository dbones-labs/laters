namespace WebApplication1;

public class WeatherForecast
{
    public DateTime Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}




public class SetupLaters : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseRouting();
            app.UseEndpoints(points =>
            {
                points.MapGet("/dave", context => context.Response.WriteAsync("Hello, world!"));
            });
            next(app);
        };
    }
}

public class SetupLaters2 : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseRouting();
            app.UseEndpoints(points =>
            {
                points.MapGet("/dave2", context => context.Response.WriteAsync("Hello, world!"));
            });
            next(app);
        };
    }
}