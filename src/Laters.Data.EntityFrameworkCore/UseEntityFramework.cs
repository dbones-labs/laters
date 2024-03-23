namespace Laters.Data.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// sets up a <see cref="LatersDbContext"/>
/// </summary>
/// <remarks>
/// please setup a connection which can be shared with the <see cref="LatersDbContext"/>
/// </remarks>
public class UseEntityFramework : StorageSetup
{

    /// <summary>
    /// the connection which will be shared for committing data.
    /// if the <see cref="ReadConnectionFactory"/> is not supplied, this connection will be used for reading as well.
    /// </summary>
    [Required]
    public Func<IServiceProvider, DbConnection>? ConnectionFactory { get; set; }

    /// <summary>
    /// supplies a connection for reading data, this is optional.
    /// </summary>
    public Func<IServiceProvider, DbConnection>? ReadConnectionFactory { get; set; }

    /// <summary>
    /// setup the options for the <see cref="LatersDbContext"/>
    /// </summary>
    [Required]
    public Action<IServiceProvider, DbConnection, DbContextOptionsBuilder>? ApplyOptions { get; set; }

    /// <summary>
    /// setup the options for the <see cref="LatersQueryDbContext"/>
    /// </summary>
    public Action<IServiceProvider, DbConnection, DbContextOptionsBuilder>? ApplyQueryOptions { get; set; }
    
    /// <summary>
    /// setup entity framework for laters
    /// </summary>
    /// <param name="collection">the ioc collection</param>
    protected override void Apply(IServiceCollection collection)
    {
        if (ApplyOptions is null)
        {
            throw new NullReferenceException($"you must supply {nameof(ApplyOptions)}");
        }
        
        if (ConnectionFactory is null)
        {
            throw new NullReferenceException($"you must supply {nameof(ConnectionFactory)}");
        }

        // if they only supply a write connection factory, we will use that for read and write
        if (ReadConnectionFactory is null)
        {
            collection.TryAddScoped<ConnectionWrapper>(provider => new WriteConnectionWrapper(ConnectionFactory(provider)));
            collection.AddScoped(provider =>
            {
                var connection =  provider.GetRequiredService<ConnectionWrapper>().Connection;
                return new WriteConnectionWrapper(connection);
            });
            collection.AddScoped(provider =>
            {
                var connection =  provider.GetRequiredService<ConnectionWrapper>().Connection;
                return new ReadConnectionWrapper(connection);
            });
        }

        // otherwise we will use the supplied factories
        else
        {
            collection.AddScoped(provider => new WriteConnectionWrapper(ConnectionFactory(provider)));
            collection.AddScoped(provider => new ReadConnectionWrapper(ReadConnectionFactory(provider)));
        }

        Action<IServiceProvider, DbContextOptionsBuilder> writeDelegate = (service, options) =>
        {
            var connection = service.GetRequiredService<WriteConnectionWrapper>().Connection;
            ApplyOptions.Invoke(service, connection, options);
        };
        
        Action<IServiceProvider, DbContextOptionsBuilder> readDelegate = ReadConnectionFactory is null
            ?  writeDelegate 
            :  (service, options) =>
        {
            var connection = service.GetRequiredService<ReadConnectionWrapper>().Connection;
            ApplyOptions.Invoke(service, connection, options);
        };
        
        collection.AddDbContext<LatersDbContext>(writeDelegate);
        collection.AddDbContext<LatersQueryDbContext>(readDelegate);
        collection.AddScoped<ISession, Session>();
    }
}