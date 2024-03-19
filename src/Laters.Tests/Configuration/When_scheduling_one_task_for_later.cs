namespace Laters.Tests.Configuration;

using Contexts.DuplicateHandler;
using Data.Marten;
using Laters.Tests.Infrastructure;
using Machine.Specifications;
using PowerAssert;

[Ignore("need to figure out how to get the exception to surface")]
[Subject("configuration")]
class When_registering_more_than_1_handler_for_a_type
{
    static DefaultTestServer _testServer;
    static Exception _result;

    Establish context = () =>
    {
        _testServer = new DefaultTestServer();
        _testServer.OverrideLaters((_, setup) =>
        {
            setup.Configuration.WorkerEndpoint = $"http://localhost/";
            setup.UseStorage<UseMarten>();
            setup.ScanForJobHandlers();
            setup.AddJobHandler<HelloDuplicateJobHandler>(); // <- this will cause the issue.
        });
        
    };

    Because of = () =>
    {
        _result = Catch.Exception(() =>
        {
            _testServer.Setup();
        });
    };
    

    It should_fail = () =>
        PAssert.IsTrue(() => _result is Exception);
    
    It should_only_process_one_job_type = () =>
        PAssert.IsTrue(() => _testServer.Monitor.CallOrder.Count == 1);

    Cleanup after = () =>
    {
        _testServer?.Dispose();
    };
}