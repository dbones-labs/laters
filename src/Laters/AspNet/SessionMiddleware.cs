namespace Laters.AspNet;

using Data;

/// <summary>
/// please supply your own, this is just to help.
/// </summary>
public class SessionMiddleware
{
    readonly RequestDelegate _next;
    
    /// <summary>
    /// setup the session middleware
    /// </summary>
    /// <param name="next">next delegate</param>
    public SessionMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// apply the session changes at the end of each request
    /// </summary>
    /// <param name="context">the http context</param>
    /// <param name="session">the main db session</param>
    /// <returns>async</returns>
    public async Task InvokeAsync(HttpContext context, ISession session)
    {
        var cancellationToken = context.RequestAborted;

        await _next(context);
        await session.SaveChanges(cancellationToken);
    }
}