namespace Laters.Data.EntityFrameworkCore;

using System.Data.Common;

public class ReadConnectionWrapper : ConnectionWrapper
{
    public ReadConnectionWrapper(DbConnection connection) : base(connection) 
    {
    }    
}