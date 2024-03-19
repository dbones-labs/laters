namespace Laters.AspNet;

using Data;

/// <summary>
/// please supply your own, this is just to help.
/// </summary>
public class SessionMiddleware
{
    readonly RequestDelegate _next;
    
    public SessionMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISession session)
    {
        await _next(context);
        await session.SaveChanges();
    }
}