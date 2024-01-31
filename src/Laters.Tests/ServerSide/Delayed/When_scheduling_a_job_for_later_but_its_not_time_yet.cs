﻿namespace Laters.Tests.ForLater.Delayed;

using Contexts.Simple;
using Infrastructure;
using Machine.Specifications;
using PowerAssert;

[Subject("delayed")]
class When_scheduling_a_job_for_later_but_its_not_time_yet
{
    static DefaultTestServer _testServer;
    static DateTime _enqueuedTime = new(2032, 01, 01, 12,12,12);
    static DateTime _firstProcessingTime = new(2032, 01, 01, 12,12, 20);
    
    static DateTime _whenToProcess= new(2032, 01, 02, 12,12,12);

    Establish context = async () =>
    {
        SystemDateTime.Set(() => _enqueuedTime);
        _testServer = new DefaultTestServer();
        _testServer.Setup();
        
        await _testServer.InScope(schedule => schedule.ForLater(new Hello { Name = "dave" },  _whenToProcess));
        await _testServer.InScope(schedule => schedule.ForLater(new Bye { Name = "dave" } ));
    };

    Because of = async () =>
    {
        SystemDateTime.Set(() => _firstProcessingTime);
        await Rig.Wait(() => _testServer.Monitor.NumberOfCallTicksFor<ByeJobHandler>() > 0);
    };

    It should_stil_yet_to_process_hello_once = () =>
        PAssert.IsTrue(() => _testServer.Monitor.NumberOfCallTicksFor<HelloJobHandler>() == 0);
    
    
    It should_processed_other_jobs = () =>
        PAssert.IsTrue(() => _testServer.Monitor.CallOrder.Count == 1);

    Cleanup after = () =>
    {
        _testServer?.Dispose();
    };
}