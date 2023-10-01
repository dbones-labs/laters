namespace Laters;

/// <summary>
/// todo complete
/// </summary>
public class ServerService : IAsyncDisposable, IDisposable
{
    readonly LatersConfiguration _configuration;
    readonly IServiceProvider _scope;
    readonly ILogger<ServerService> _logger;
    public bool IsLeader { get; set; } = false;

    ContinuousLambda _heartbeat;
    ContinuousLambda _electServer;
    
    string _thisServerId = Guid.NewGuid().ToString("D");
    const string _electedLeaderName = "leader";

    public ServerService(
        LatersConfiguration configuration,
        IServiceProvider scope, 
        ILogger<ServerService> logger)
    {
        _configuration = configuration;
        _scope = scope;
        _logger = logger;
        
        //_heartbeat = new ContinuousLambda(ServerHeartBeat, new TimeTrigger(TimeSpan.FromSeconds(3)));
        _electServer = new ContinuousLambda(async () => await ElectLeader(), new TimeTrigger(TimeSpan.FromSeconds(3)));
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
        //session.Delete<Server>(_thisServerId);
        
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
    /// see if we need to promote this instance to leader
    /// </summary>
    /// <remarks>
    /// this is a first one to succeed will become the leader
    /// </remarks>
    protected virtual async Task ElectLeader()
    {
        using var loggerScope = _logger.BeginScope($"Server Election: {_thisServerId}");
        //setup our instance
        bool isLeader = false;
        using var workingScope = _scope.CreateScope();
        await using var session = workingScope.ServiceProvider.GetRequiredService<ISession>();

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
            var timeout = leader.Updated.AddSeconds(_configuration.LeaderTimeToLiveInSeconds);
            if (timeout > SystemDateTime.UtcNow)
            {
                //all is good
                return;
            }

            //leader is old, lets try and promote us
            leader.ServerId = _thisServerId;
            leader.Updated = SystemDateTime.UtcNow;
        }

        try
        { 
            isLeader = true;
            _logger.LogInformation("up for Leader (re)-election");
            await session.SaveChanges();
            _logger.LogInformation("promoted to new leader");
        }
        catch (ConcurrencyException exception)
        {
            _logger.LogWarning("Unable to promote to leader, check to see if there is another leader");
        }
        
    }

    public async ValueTask DisposeAsync()
    {
        await CleanUp();
        _heartbeat?.Dispose();
        _electServer?.Dispose();
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().Wait(200);
    }
}