namespace Laters.Tests.Minimal;

using AspNet;
using ClientProcessing;
using Contexts.Minimal;
using Infrastructure;
using Machine.Specifications;
using Mnimal;
using PowerAssert;


[Subject("minimal")]
class When_using_a_minimal_handler_with_a_single_job
{
    static DefaultTestServer _testServer;

    Establish context = () =>
    {
        _testServer = new DefaultTestServer();
        _testServer.OverrideBuilder(app =>
        {
            var marker = new MinimalHello();
            app.UseLaters();
            
            app.MapHandler<Hello>(async (JobContext<Hello> ctx, TestMonitor monitor, ILogger<MinimalHello> logger) =>
            {
                monitor.AddCallTick(marker);
                monitor.Observed.Add("ctx", ctx);
                monitor.Observed.Add("logger", logger);
            });
        });
        _testServer.Setup();
    };

    Because of = async () =>
    {
        await _testServer.InScope(schedule => schedule.ForLater(new Hello { Name = "dave" } ));
        await Rig.Wait(() => _testServer.Monitor.NumberOfCallTicksFor<MinimalHello>() >= 1);
    };

    It should_only_be_processed_once = () =>
        PAssert.IsTrue(() => _testServer.Monitor.NumberOfCallTicksFor<MinimalHello>() == 1);
    
    It should_inject_the_context = () =>
        PAssert.IsTrue(() => _testServer.Monitor.GetObserved<JobContext<Hello>>("ctx") != null);
    
    It should_inject_params_from_container = () =>
        PAssert.IsTrue(() => _testServer.Monitor.GetObserved<ILogger<MinimalHello>>("logger") != null);

    Cleanup after = () =>
    {
        _testServer?.Dispose();
    };
}