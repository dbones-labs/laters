namespace Laters;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// Leader election is where we signal the leader
/// </summary>
public class LeaderElectionService : INotifyPropertyChanged, IAsyncDisposable, IDisposable
{
    readonly LeaderContext _leaderContext;
    readonly LatersConfiguration _configuration;
    readonly IServiceProvider _scope;
    readonly ILogger<LeaderElectionService> _logger;
    
    ContinuousLambda _heartbeat;
    ContinuousLambda _electServer;
    
    const string _electedLeaderName = "leader";

    public LeaderElectionService(
        LeaderContext leaderContext,
        LatersConfiguration configuration,
        IServiceProvider scope, 
        ILogger<LeaderElectionService> logger)
    {
        _leaderContext = leaderContext;
        _configuration = configuration;
        _scope = scope;
        _logger = logger;
        
        _electServer = new ContinuousLambda(nameof(ElectLeader), async () => await ElectLeader(), new TimeTrigger(TimeSpan.FromSeconds(3)));
    }
    
    /// <summary>
    /// if this running service/application instance is the leader
    /// </summary>
    public bool IsLeader { get; set; } = false;


    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initialize the Election component");
        _electServer.Start(cancellationToken);
    }

    public async Task CleanUp(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CleanUp the Election component");
        using var workingScope = _scope.CreateScope();
        using var session = _scope.GetRequiredService<ISession>();
        
        //check leader
        var leader = await session.GetById<Leader>(_electedLeaderName);
        var isCurrentLeader = leader is not null && _leaderContext.IsThisServer(leader);
        if (isCurrentLeader)
        {
            session.Delete<Leader>(_electedLeaderName);   
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
        try
        {
            using var _ = _logger.BeginScope(new Dictionary<string, string>
            {
                { "ServerId", _leaderContext.ServerId },
                { "Action", nameof(ElectLeader) }
            });
            
            bool isLeader = false;
            using var workingScope = _scope.CreateScope();
            using var session = workingScope.ServiceProvider.GetRequiredService<ISession>();

            var leader = await session.GetById<Leader>(_electedLeaderName);
            _leaderContext.Leader = leader;
            if (leader is null)
            {
                _logger.LogInformation("no leader found, trying to promote this server");
                //first time run! we have no leader, lets provide us
                leader = new Leader()
                {
                    Id = _electedLeaderName,
                    Updated = SystemDateTime.UtcNow,
                    ServerId = _leaderContext.ServerId
                };
                
                session.Store(leader);
            }
            else
            {
                _logger.LogInformation("updating our leader registration");
                //confirm the current leader
                var timeout = leader.Updated.AddSeconds(_configuration.LeaderTimeToLiveInSeconds);
                if (timeout > SystemDateTime.UtcNow)
                {
                    //all is good
                    return;
                }

                //leader is old, lets try and promote us
                leader.ServerId = _leaderContext.ServerId;
                leader.Updated = SystemDateTime.UtcNow;
            }

            try
            { 
                isLeader = true;
                _logger.LogInformation("up for Leader (re)-election");
                await session.SaveChanges();
                _leaderContext.Leader = leader; //was updated
                _logger.LogInformation("updated leader info in store");
            }
            catch (ConcurrencyException exception)
            {
                isLeader = false;
                _logger.LogWarning("Unable to promote to leader, check to see if there is another leader");
            }

            if (isLeader != IsLeader)
            {
                var message = isLeader ? "Promoted to leader" : "No longer leader";
                _logger.LogInformation(message);
                IsLeader = isLeader;
                OnPropertyChanged(nameof(IsLeader));
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e,"Unable to update the data with leader information");
            //_hostApplicationLifetime.StopApplication();
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


public class LeaderContext
{
    public Leader? Leader { get; set; }
    public string ServerId { get; set; } = Guid.NewGuid().ToString("D");

    public bool IsLeader => ServerId.Equals(Leader?.ServerId);

    public bool IsThisServer(Leader currentLeader)
    {
        return ServerId.Equals(currentLeader?.ServerId);
    }
}