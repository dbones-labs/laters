namespace Laters.Data.EntityFrameworkCore;

using System.Data.Common;

/// <summary>
/// this allows us to handle connections between read and write for the DbContext
/// </summary>
public abstract class ConnectionWrapper
{
    /// <summary>
    /// the connection to use for a DbContext
    /// </summary>
    public DbConnection Connection { get; }

    /// <summary>
    /// creates the wrapper for the connection
    /// </summary>
    /// <param name="connection">the connection to use</param>
    public ConnectionWrapper(DbConnection connection)
    {
        Connection = connection;
    }
}