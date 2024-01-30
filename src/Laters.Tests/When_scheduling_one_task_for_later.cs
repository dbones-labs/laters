namespace Laters.Tests;

using Contexts.Simple;
using Infrastructure;
using Machine.Specifications;
using PowerAssert;

[Subject("Later")]
class When_scheduling_one_task_for_later
{
    static DefaultTestServer _testServer;

    Establish context = () =>
    {
        _testServer = new DefaultTestServer();
        _testServer.Setup();
    };

    Because of = async () =>
    {
        await _testServer.InScope(schedule => schedule.ForLater(new Hello() { Name = "dave" }));
            
        // we need to wait to ensure we only process it once
        await Task.Delay(TimeSpan.FromSeconds(5)); 
        await Rig.Wait(() => _testServer.Monitor.NumberOfCallTicksFor<HelloJobHandler>() > 0);
    };

    It should_only_be_processed_once = () =>
        PAssert.IsTrue(() => _testServer.Monitor.NumberOfCallTicksFor<HelloJobHandler>() == 1);
    
    It should_only_process_one_job_type = () =>
        PAssert.IsTrue(() => _testServer.Monitor.CallOrder.Count == 1);

    Cleanup after = () =>
    {
        _testServer?.Dispose();
    };
}