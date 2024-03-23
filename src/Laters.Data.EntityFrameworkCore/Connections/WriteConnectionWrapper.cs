namespace Laters.Data.EntityFrameworkCore;

using System.Data.Common;


/// <summary>
/// this is the write connection (it can also be used for read)
/// </summary>
public class WriteConnectionWrapper : ConnectionWrapper
{
    /// <summary>
    /// create the write connection wrapper
    /// </summary>
    /// <param name="connection">connection for write operations</param>
    /// <returns></returns>
    public WriteConnectionWrapper(DbConnection connection) : base(connection) 
    {
    }    
}