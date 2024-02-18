namespace Laters.Tests.ServerSide.Windows;

using ClientProcessing;
using ServerProcessing;
using Laters.Tests.Contexts.Minimal;
using Infrastructure;
using Infrastucture;
using Machine.Specifications;
using PowerAssert;
using Hello = Contexts.Minimal.Hello;

[Subject("window")]
class When_queueing_over_the_global_max
{
    static DefaultTestServer? _sut;
    static LeaderContext? _context;

    static bool _windowOne = false;
    static bool _windowTwo = false;
    static bool _waitedBetweenWindows = false;
    
    static DateTime _firstSlice = new(1999, 1, 1, 1, 0, 0);
    static DateTime _secondSlice = new(1999, 1, 1, 1, 0, 3);
    static DateTime _thirdSlice = new(1999, 1, 1, 1, 0, 4);
    static DateTime _forthSlice = new(1999, 1, 1, 1, 0, 6);
    
    
    Establish context = async () =>
    {
        _sut = new DefaultTestServer();
        _sut.AdditionalOverrideLaters((builderContext, setup) =>
        {
            setup.Configuration.Windows.ConfigureGlobal(3, 5);
            //setup.Configuration.NumberOfProcessingThreads = 9;
        });
        _sut.MinimalApi(app =>
        {
            var marker = new MinimalHello();
            app.MapHandler<Hello>(async (JobContext<Hello> ctx, TestMonitor monitor) =>
            {
                monitor.AddCallTick(marker);
            });
        });
        _sut.Setup();
        
        SystemDateTime.Set(()=> _firstSlice);
        
        await _sut.InScope(schedule =>
        {
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            schedule.ForLater(new Hello { Name = "dave" });
            return Task.CompletedTask;
        });
        
        SystemDateTime.Set(()=> _secondSlice);
    };

    Because of = async () =>
    {
        await Rig.Wait(() => _sut!.Monitor.NumberOfCallTicksFor<MinimalHello>() >= 3, TimeSpan.FromSeconds(15));
        _windowOne = true;

        SystemDateTime.Set(()=> _thirdSlice);
        
        //we wait, to ensure 
        //Func<Task> shouldFail = () =>  Rig.Wait(() => _sut!.Monitor.NumberOfCallTicksFor<MinimalHello>() >= 4); // for debugging 
        //Func<Task> shouldFail = () =>  Rig.Wait(() => _sut!.Monitor.NumberOfCallTicksFor<MinimalHello>() >= 4, TimeSpan.FromSeconds(5));
        var exception = await Catch.ExceptionAsync (() => Rig.Wait(() => _sut!.Monitor.NumberOfCallTicksFor<MinimalHello>() >= 4, TimeSpan.FromSeconds(3)));
        _waitedBetweenWindows = exception is not null;
        
        SystemDateTime.Set(()=> _forthSlice);
        
        await Rig.Wait(() => _sut!.Monitor.NumberOfCallTicksFor<MinimalHello>() == 6, TimeSpan.FromSeconds(10));
        _windowTwo = true;
    };

    It should_process_items_in_tumberling_windows = () =>
        PAssert.IsTrue(() => _windowOne && _windowTwo && _waitedBetweenWindows);
    
    Cleanup after = () =>
    {
        _sut?.Dispose();
    };
}