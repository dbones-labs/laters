namespace Laters.Data.EntityFrameworkCore;

using System.Data.Common;

public class WriteConnectionWrapper : ConnectionWrapper
{
    public WriteConnectionWrapper(DbConnection connection) : base(connection) 
    {
    }    
}