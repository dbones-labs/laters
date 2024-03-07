namespace Laters.Tests.ServerSide.Delayed;

using Laters.Infrastucture;
using Laters.Tests.Contexts.Simple;
using Laters.Tests.Infrastructure;
using Machine.Specifications;
using PowerAssert;

[Subject("delayed")]
class When_scheduling_a_job_for_later
{
    static DefaultTestServer _testServer;
    static DateTime _enqueuedTime =         new(2032, 01, 01, 12,12, 12);
    static DateTime _firstProcessingTime =  new(2032, 01, 01, 12,12, 20);
    
    static DateTime _whenToProcess=         new(2032, 01, 02, 12,12, 12);
    static DateTime _secondProcessingTime = new(2032, 01, 02, 12,12, 20);
    

    Establish context = async () =>
    {
        SystemDateTime.Set(() => _enqueuedTime);
        _testServer = new DefaultTestServer();
        _testServer.Setup();
        
        await _testServer.InScope(schedule => schedule.ForLater(new Hello { Name = "dave" },  _whenToProcess));
        await _testServer.InScope(schedule => schedule.ForLater(new Bye { Name = "dave" } ));
        
        SystemDateTime.Set(() => _firstProcessingTime);
        await Rig.Wait(() => _testServer.Monitor.NumberOfCallTicksFor<ByeJobHandler>() > 0);
    };

    Because of = async () =>
    {
        SystemDateTime.Set(() => _secondProcessingTime);
        await Rig.Wait(() => _testServer.Monitor.NumberOfCallTicksFor<HelloJobHandler>() > 0, TimeSpan.FromMinutes(3));
    };

    It should_have_processed_hello_once = () =>
        PAssert.IsTrue(() => _testServer.Monitor.NumberOfCallTicksFor<HelloJobHandler>() == 1);
    
    
    It should_process_two_job_types = () =>
        PAssert.IsTrue(() => _testServer.Monitor.CallOrder.Count == 2);

    Cleanup after = () =>
    {
        _testServer?.Dispose();
    };
}