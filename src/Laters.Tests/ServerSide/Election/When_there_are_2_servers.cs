namespace Laters.Tests.ServerSide.Election;

using ClientProcessing;
using Contexts.Minimal;
using Infrastructure;
using Infrastucture;
using Laters.Configuration;
using Laters.Minimal.Application;
using Machine.Specifications;
using PowerAssert;
using ServerProcessing;
using Hello = Contexts.Minimal.Hello;

[Subject("election")]
class When_there_are_2_servers
{
    static DefaultTestServer? _leader1;
    static DefaultTestServer? _leader2;
    static DefaultTestServer? _worker;
    static LeaderContext? _context;
    
    static DateTime _time = new(1999, 1, 1, 1, 1, 0);
    
    static string _leaderKey = "leaderId";
    
    Establish context = async () =>
    {
        SystemDateTime.Set(()=> _time);
        _worker = new DefaultTestServer(role: Roles.Worker);
        _worker.MinimalApi(app =>
        {
            var marker = new MinimalHello();
            app.MapHandler<Hello>(async (JobContext<Hello> ctx, TestMonitor monitor) =>
            {
                monitor.Observed.TryAdd(_leaderKey, ctx.ServerRequested!.LeaderId);
                monitor.AddCallTick(marker);
            });
        });
        _worker.Setup();
        
        await _worker.InScope(schedule =>
        {
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
        });
    };

    Because of = async () =>
    {
        var port = _worker.Port;

        var setup1 = () =>
        {
            _leader1 = new DefaultTestServer(role: Roles.Leader, data: TestData.Keep);
            _leader1.AdditionalOverrideLaters((_, setup) => setup.Configuration.WorkerEndpoint = $"http://localhost:{port}/");
            _leader1.Setup();
        };

        var setup2 = () =>
        {
            _leader2 = new DefaultTestServer(role: Roles.Leader, data: TestData.Keep);
            _leader2.AdditionalOverrideLaters((_, setup) => setup.Configuration.WorkerEndpoint = $"http://localhost:{port}/");
            _leader2.Setup();
        };
        var t1 = new Task(setup1);
        var t2 = new Task(setup2);
        t1.Start();
        t2.Start();
            
        await Task.WhenAll(t1, t2);

        //await Task.Delay(500); //allow time for these servers to argue
        
        await Rig.Wait(() => _worker!.Monitor.NumberOfCallTicksFor<MinimalHello>() >= 4, TimeSpan.FromSeconds(9999));

        _context = _leader1.Leader.IsLeader ? _leader1.Leader : _leader2.Leader;
    };

    It should_only_have_one_selected_leader = () =>
        PAssert.IsTrue(() => _leader1!.Leader.IsLeader != _leader2!.Leader.IsLeader);
    
    
    It should_be_the_same_for_the_client = () =>
        PAssert.IsTrue(() => (string)_worker!.Monitor.Observed[_leaderKey] == _context!.ServerId);
    

    Cleanup after = () =>
    {
        _leader1?.Dispose();
        _leader2?.Dispose();
        _worker?.Dispose();
    };
}