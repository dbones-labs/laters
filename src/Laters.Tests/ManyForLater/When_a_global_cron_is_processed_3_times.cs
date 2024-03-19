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
class When_a_global_cron_is_processed_3_times
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
        SystemDateTime.Set(()=> _firstSlice);
        _sut = new DefaultTestServer();
        _sut.AdditionalOverrideLaters((builderContext, setup) =>
        {
            setup.AddSetupSchedule<GlobalSchedules>();
        });
        
        _sut.OverrideBuilder(app =>
        {
            var marker = new MinimalHello();
            app.UseLaters();
            
            app.MapHandler<Hello>(async (JobContext<Hello> ctx, TestMonitor monitor) =>
            {
                monitor.AddCallTick(marker);
            });
        });
        await _sut.Setup();
        await Task.Delay(500);
    };

    Because of = async () =>
    {
        Exception? caught = null;
        
        SystemDateTime.Set(()=> _secondSlice);
        caught = await Rig.TryWait(() => _sut.Monitor.NumberOfCallTicksFor<MinimalHello>() >= 1, TimeSpan.FromSeconds(9999));
        await Task.Delay(50); // smh
        if (caught is null) _task1Competed = true;
        
        SystemDateTime.Set(()=> _thirdSlice);
        caught = await Rig.TryWait(() => _sut.Monitor.NumberOfCallTicksFor<MinimalHello>() >= 2);
        await Task.Delay(50);
        if (caught is null) _task2Competed = true;
        
        SystemDateTime.Set(()=> _forthSlice);
        caught = await Rig.TryWait(() => _sut.Monitor.NumberOfCallTicksFor<MinimalHello>() >= 3);
        await Task.Delay(50);
        if (caught is null) _task3Competed = true;
    };

    It should_process_the_cron_once = () =>
        PAssert.IsTrue(() => _task1Competed);
    
    It should_process_the_cron_twice = () =>
        PAssert.IsTrue(() => _task2Competed);
    
    It should_process_the_cron_thrice = () =>
        PAssert.IsTrue(() => _task3Competed);
    

    Cleanup after = () =>
    {
        _sut?.Dispose();
    };
    
    
    [Laters.Configuration.Ignore]
    public class GlobalSchedules : ISetupSchedule
    {
        public void Configure(IScheduleCron scheduleCron)
        {
            var midnight = "0 0 * * *";
            scheduleCron.ManyForLater("greetings", new Hello { Name = "dave" },  midnight);
        }
    }
}