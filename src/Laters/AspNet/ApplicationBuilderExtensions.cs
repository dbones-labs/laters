namespace Laters.AspNet;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// sets up the middleware that Laters requires to process jobs over http
    /// </summary>
    /// <param name="app">your WebApplication</param>
    /// <param name="commit">choose if you want to apply commit on any state changes, this is only to help, you should supply your own</param>
    public static void UseLaters(this IApplicationBuilder app, CommitStrategy commit = CommitStrategy.UseApplicationProvided)
    {
        app.UseMiddleware<ClientMiddleware>();
        if (commit == CommitStrategy.SupplyMiddleware)
        {
            app.UseMiddleware<SessionMiddleware>();
        }
    }
}


/// <summary>
/// who is accountable for calling commit/save-changes on the unit of work.
/// </summary>
public enum CommitStrategy
{
    /// <summary>
    /// you will handle commit/save-changes
    /// </summary>
    UseApplicationProvided,
    
    /// <summary>
    /// use the laters provided middleware to save changes on ASPNET endpoints
    /// </summary>
    SupplyMiddleware
}