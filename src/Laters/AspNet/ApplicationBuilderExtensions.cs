namespace Laters.AspNet;

public static class ApplicationBuilderExtensions
{
    public static void UseLaters(this IApplicationBuilder app)
    {
        app.UseMiddleware<ClientMiddleware>();
    }
}