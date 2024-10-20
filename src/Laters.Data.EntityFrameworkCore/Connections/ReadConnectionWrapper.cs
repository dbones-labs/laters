namespace Laters.Data.EntityFrameworkCore;

using System.Data.Common;


/// <summary>
/// this is the read only connection wrapper
/// </summary>
public class ReadConnectionWrapper : ConnectionWrapper
{
    /// <summary>
    /// create the read connection wrapper
    /// </summary>
    /// <param name="connection">connection for read only operations</param>
    public ReadConnectionWrapper(DbConnection connection) : base(connection) 
    {
    }    
}