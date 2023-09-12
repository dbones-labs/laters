namespace Laters;

/// <summary>
/// todo complete
/// </summary>
public class ServerService : IAsyncDisposable, IDisposable
{
    private readonly LatersConfiguration _configuration;
    private readonly IServiceProvider _scope;
    private readonly ILogger<ServerService> _logger;

    private ContinuousLambda _heartbeat;
    private ContinuousLambda _electServer;


    private string _thisServerId = Guid.NewGuid().ToString("D");
    private const string _electedLeaderName = "leader";

    public ServerService(
        LatersConfiguration configuration,
        IServiceProvider scope, 
        ILogger<ServerService> logger)
    {
        _configuration = configuration;
        _scope = scope;
        _logger = logger;
        
        _heartbeat = new ContinuousLambda(ServerHeartBeat, new TimeTrigger(TimeSpan.FromSeconds(3)));
        _electServer = new ContinuousLambda(ElectLeader, new TimeTrigger(TimeSpan.FromSeconds(3)));
    }

    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        //_heartbeat.Start(cancellationToken);
        _electServer.Start(cancellationToken);
    }

    public async Task CleanUp(CancellationToken cancellationToken = default)
    {
        using var workingScope = _scope.CreateScope();
        await using var session = _scope.GetRequiredService<ISession>();
        
        //remove self
        session.Delete<Server>(_thisServerId);
        
        //check leader
        var leader = await session.GetById<LeaderServer>(_electedLeaderName);
        var isCurrentLeader = leader is not null && _thisServerId.Equals(leader.ServerId);
        if (isCurrentLeader)
        {
            session.Delete<LeaderServer>(_electedLeaderName);   
        }

        await session.SaveChanges();
    }
    
    /// <summary>
    /// heartbeat, letting everyone know we are alive
    /// </summary>
    public virtual async Task ServerHeartBeat()
    {
        //setup our instance
        using var workingScope = _scope.CreateScope();
        await using var session = _scope.GetRequiredService<ISession>();

        var thisServer = await session.GetById<Server>(_thisServerId);
        if (thisServer is null)
        {
            thisServer = new()
            {
                Id = _thisServerId
            };
            session.Store(thisServer);
        }
        
        //we always update the datetime, to keep a track of
        //any running instances
        thisServer.Updated = SystemDateTime.UtcNow;

        await session.SaveChanges();
    }

    /// <summary>
    /// see if we need to promote this instance to leader
    /// </summary>
    /// <remarks>
    /// this is a first one to succeed will become the leader
    /// </remarks>
    public virtual async Task ElectLeader()
    {
        //setup our instance
        using var workingScope = _scope.CreateScope();
        await using var session = _scope.GetRequiredService<ISession>();

        var leader = await session.GetById<LeaderServer>(_electedLeaderName);
        if (leader is null)
        {
            //first time run! we have no leader, lets provide us
            leader = new LeaderServer()
            {
                Id = _electedLeaderName,
                Updated = SystemDateTime.UtcNow,
                ServerId = _thisServerId
            };

            session.Store(leader);
        }
        else
        {
            //confirm the current leader
            var timeout = SystemDateTime.UtcNow.AddSeconds(_configuration.LeaderTimeToLiveInSeconds);
            if (leader.Updated < timeout)
            {
                //all is good
                return;
            }

            //leader is old, lets try and promote us
            leader.ServerId = _thisServerId;
            leader.Updated = SystemDateTime.UtcNow;
        }

        await session.SaveChanges();
    }

    public async Task ClearServers()
    {
        using var workingScope = _scope.CreateScope();
        await using var session = _scope.GetRequiredService<ISession>();
        
        var servers = await session.GetServers();
        var timeout = SystemDateTime.UtcNow.AddSeconds(_configuration.LeaderTimeToLiveInSeconds);
        foreach (var server in servers)
        {
            if (server.Updated > timeout)
            {
                session.Delete<Server>(server.Id);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CleanUp();
        _heartbeat.Dispose();
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().Wait(200);
    }
}