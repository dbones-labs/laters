﻿namespace Laters.Tests.ForLater.Simple;

using Contexts.Simple;
using Laters.Tests.Infrastructure;
using Machine.Specifications;
using PowerAssert;

/// <summary>
/// at best we only really want to process a job once, under ideal conditions.
/// </summary>
[Tags("quality")]
[Subject("for-later")]
class When_scheduling_2_tasks_for_later
{
    static DefaultTestServer _testServer;

    Establish context = () =>
    {
        _testServer = new DefaultTestServer();
        _testServer.Setup();
    };

    Because of = async () =>
    {
        await _testServer.InScope(schedule => schedule.ForLater(new Hello { Name = "dave" } ));
        await _testServer.InScope(schedule => schedule.ForLater(new Hello { Name = "dave" } ));
        await _testServer.InScope(schedule => schedule.ForLater(new Hello { Name = "dave" } ));
        await _testServer.InScope(schedule => schedule.ForLater(new Hello { Name = "dave" } ));
        await _testServer.InScope(schedule => schedule.ForLater(new Hello { Name = "dave" } ));
        
        // we need to wait to ensure we only process it once
        await Rig.Wait(() => _testServer.Monitor.NumberOfCallTicksFor<HelloJobHandler>() >= 5);
    };

    It should_only_be_processed_once = () =>
        PAssert.IsTrue(() => _testServer.Monitor.NumberOfCallTicksFor<HelloJobHandler>() == 5);
    
    It should_only_process_one_job_type = () =>
        PAssert.IsTrue(() => _testServer.Monitor.CallOrder.Count == 5);

    Cleanup after = () =>
    {
        _testServer?.Dispose();
    };
}