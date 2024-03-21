namespace Laters.Tests.ForLater.Simple;

using Contexts.Simple;
using Laters.Tests.Infrastructure;
using Machine.Specifications;
using PowerAssert;

[Tags("quality")]
[Subject("for-later")]
class When_scheduling_2_different_task_types
{
    static DefaultTestServer _testServer = null!;

    Establish context = async () =>
    {
        _testServer = new DefaultTestServer();
        await _testServer.Setup();
    };

    Because of = async () =>
    {
        await _testServer.InScope(schedule => schedule.ForLater(new Hello { Name = "dave" } ));
        await _testServer.InScope(schedule => schedule.ForLater(new Bye { Name = "dave" } ));
        
        // we need to wait to ensure we only process it once
        await Task.Delay(TimeSpan.FromSeconds(5)); 
        await Rig.Wait(() => _testServer.Monitor.NumberOfCallTicksFor<HelloJobHandler>() > 0);
        await Rig.Wait(() => _testServer.Monitor.NumberOfCallTicksFor<ByeJobHandler>() > 0);
    };

    It should_have_processed_hello_once = () =>
        PAssert.IsTrue(() => _testServer.Monitor.NumberOfCallTicksFor<HelloJobHandler>() == 1);
    
    It should_have_processed_bye_once = () =>
        PAssert.IsTrue(() => _testServer.Monitor.NumberOfCallTicksFor<ByeJobHandler>() == 1);
    
    It should_process_two_job_types = () =>
        PAssert.IsTrue(() => _testServer.Monitor.CallOrder.Count == 2);

    Cleanup after = () =>
    {
        _testServer?.Dispose();
    };
}