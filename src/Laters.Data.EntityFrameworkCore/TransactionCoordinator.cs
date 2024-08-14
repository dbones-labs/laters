namespace Laters.Data.EntityFrameworkCore;

using System.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// this will coordinate transactions between the <see cref="LatersDbContext"/> and the application's <see cref="DbContext"/>
/// </summary>
public class TransactionCoordinator
{
    readonly LatersDbContext _latersDbContext;
    readonly ApplicationDbContextWrapper _applicationDbContextWrapper;
    readonly WriteConnectionWrapper _connectionWrapper;

    /// <summary>
    /// create the transaction coordinator
    /// </summary>
    /// <param name="latersDbContext">laters context</param>
    /// <param name="applicationDbContextWrapper">applications context</param>
    /// <param name="connectionWrapper">the write connection</param>
    public TransactionCoordinator(
        LatersDbContext latersDbContext, 
        ApplicationDbContextWrapper applicationDbContextWrapper, 
        WriteConnectionWrapper connectionWrapper)
    {
        _latersDbContext = latersDbContext;
        _applicationDbContextWrapper = applicationDbContextWrapper;
        _connectionWrapper = connectionWrapper;
    }    

    /// <summary>
    ///  commits the transaction
    /// </summary>
    public async Task Commit(CancellationToken cancellationToken = default)
    {

        if(_connectionWrapper.Connection.State != ConnectionState.Open) 
        {
            await _connectionWrapper.Connection.OpenAsync(cancellationToken);
        }

        using var tx = _connectionWrapper.Connection.BeginTransaction(IsolationLevel.Serializable);

        _latersDbContext.Database.UseTransaction(tx);
        _applicationDbContextWrapper.DbContext.Database.UseTransaction(tx);

        await _applicationDbContextWrapper.DbContext.SaveChangesAsync(cancellationToken);
        await _latersDbContext.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);
    }
}