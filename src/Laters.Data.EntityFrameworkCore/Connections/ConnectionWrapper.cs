namespace Laters.Data.EntityFrameworkCore;

using System.Data.Common;

public abstract class ConnectionWrapper
{
    public DbConnection Connection { get; }

    public ConnectionWrapper(DbConnection connection)
    {
        Connection = connection;
    }
}