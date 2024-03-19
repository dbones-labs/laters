namespace Laters.Tests.Minimal;

using AspNet;
using ClientProcessing;
using Contexts.Minimal;
using Infrastructure;
using Laters.Infrastructure;
using Laters.Minimal.Application;
using Machine.Specifications;
using PowerAssert;

[Subject("cron")]
class When_a_cron_is_processed_3_times_and_keep_orphins_after_first
{
    static DefaultTestServer _sut;
    
    static DateTime _firstSlice = new(1999, 1, 1, 1, 1, 0); //start (insert)
    static DateTime _secondSlice = new(1999, 1, 2, 1, 1, 0); //first batch
    static DateTime _thirdSlice = new(1999, 1, 3, 1, 1, 0); //second batch
    static DateTime _forthSlice = new(1999, 1, 4, 1, 1, 0); //third batch

    static bool _task1Competed;
    static bool _task2Competed;
    static bool _task3Competed;

    Establish context = async () =>
    {
        _sut = new DefaultTestServer();
        _sut.OverrideBuilder(app =>
        {
            var marker = new MinimalHello();
            app.UseLaters();
            
            app.MapHandler<Hello>(async (JobContext<Hello> ctx, TestMonitor monitor) =>
            {
                monitor.AddCallTick(marker);
            });
        });
        _sut.Setup();
        
        Exception? caught = null;
        SystemDateTime.Set(()=> _firstSlice);
        var midnight = "0 0 * * *";
        await _sut.InScope(schedule => schedule.ManyForLater("greetings", new Hello { Name = "dave" },  midnight));
        
        SystemDateTime.Set(()=> _secondSlice);
        caught = await Rig.TryWait(() => _sut.Monitor.NumberOfCallTicksFor<MinimalHello>() >= 1);
        await Task.Delay(50); // smh
        if (caught is null) _task1Competed = true;
    };

    Because of = async () =>
    {
        Exception? caught = null;
        await _sut.InScope(schedule => schedule.ForgetAboutAllOfIt<MinimalHello>("greetings", false));
        
        SystemDateTime.Set(()=> _thirdSlice);
        caught = await Rig.TryWait(() => _sut.Monitor.NumberOfCallTicksFor<MinimalHello>() >= 2);
        await Task.Delay(50);
        if (caught is null) _task2Competed = true;
        
        SystemDateTime.Set(()=> _forthSlice);
        caught = await Rig.TryWait(() => _sut.Monitor.NumberOfCallTicksFor<MinimalHello>() >= 3, TimeSpan.FromMilliseconds(500));
        await Task.Delay(50);
        if (caught is null) _task3Competed = true;
    };

    It should_process_the_cron_once = () =>
        PAssert.IsTrue(() => _task1Competed);
    
    It should_process_the_cron_twice = () =>
        PAssert.IsTrue(() => _task2Competed);
    
    It should_not_process_the_cron_thrice = () =>
        PAssert.IsTrue(() => !_task3Competed);
    

    Cleanup after = () =>
    {
        _sut?.Dispose();
    };
}