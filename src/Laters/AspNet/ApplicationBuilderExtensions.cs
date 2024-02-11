namespace Laters.AspNet;

public static class ApplicationBuilderExtensions
{
    
    /// <summary>
    /// sets up the middleware that Laters requires to process jobs over http
    /// </summary>
    /// <param name="app">your WebApplication</param>
    public static void UseLaters(this IApplicationBuilder app)
    {
        app.UseMiddleware<ClientMiddleware>();
    }
}