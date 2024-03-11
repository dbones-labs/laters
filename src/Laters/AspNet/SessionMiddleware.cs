namespace Laters.AspNet;

using Data;

/// <summary>
/// please supply your own, this is just to help.
/// </summary>
public class SessionMiddleware
{
    readonly RequestDelegate _next;
    readonly ISession _session;
    
    public SessionMiddleware(
        RequestDelegate next, 
        ISession session)
    {
        _next = next;
        _session = session;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        await _session.SaveChanges();
    }
}