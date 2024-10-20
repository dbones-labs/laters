﻿namespace Laters.ServerProcessing;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Configuration;
using Data;
using Infrastructure;
using Laters.Infrastructure.Telemetry;
using Models;
using Triggers;

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

        var timeTrigger = new TimeTrigger(TimeSpan.FromSeconds(configuration.CheckLeaderInSeconds));
        
        _electServer = new ContinuousLambda(nameof(ElectLeader), async () => await ElectLeader(), timeTrigger);
    }
    
    /// <summary>
    /// if this running service/application instance is the leader
    /// </summary>
    public bool IsLeader { get; set; } = false;


    public void Initialize(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initialize the Election component");
        _electServer.Start(cancellationToken);
    }

    public async Task CleanUp(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("CleanUp the Election component");
            using var workingScope = _scope.CreateScope();
            var session = _scope.GetRequiredService<ISession>();

            //check leader
            var leader = await session.GetById<Leader>(_electedLeaderName);
            var isCurrentLeader = leader is not null && _leaderContext.IsThisServer(leader);
            if (isCurrentLeader)
            {
                session.Delete<Leader>(_electedLeaderName);
            }

            await session.SaveChanges();
        }
        catch (Exception e)
        {
            //the ioc container may be disposed already.
        }
        
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
                { Telemetry.ServerId, _leaderContext.ServerId },
                { Telemetry.Action , nameof(ElectLeader) }
            });
            
            bool isLeader = false;
            using var workingScope = _scope.CreateScope();
            var session = workingScope.ServiceProvider.GetRequiredService<ISession>();

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
                
                var delay = _leaderContext.IsLeader 
                    ? (2.0 * _configuration.LeaderTimeToLiveInSeconds) / 3.0
                    : _configuration.LeaderTimeToLiveInSeconds;
                
                var timeout = leader.Updated.AddSeconds(delay);
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