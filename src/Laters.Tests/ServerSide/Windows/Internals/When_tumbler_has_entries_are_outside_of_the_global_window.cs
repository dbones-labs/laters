namespace Laters.Tests.ServerSide.Windows;

using Infrastructure;
using Infrastucture;
using Laters.Configuration;
using Machine.Specifications;
using PowerAssert;
using ServerProcessing.Windows;

[Subject("window")]
[Tags("class-test")]
class When_tumbler_has_entries_are_outside_of_the_global_window
{
    static DefaultTumbler? _sut;

    static DateTime _firstSlice = new(1999, 1, 1, 1, 0, 0);
    static DateTime _secondSlice = new(1999, 1, 1, 1, 0, 5);
    static DateTime _thirdSlice = new(1999, 1, 1, 1, 0, 10);
    static DateTime _observedSlice = new(1999, 1, 1, 1, 0, 15);

    Establish context = () =>
    {
        var config = new LatersConfiguration();
        config.Windows.ConfigureGlobal(303, 11);
        _sut = new DefaultTumbler(config);
        _sut.Initialize(new CancellationToken());

        SystemDateTime.Set(() => _firstSlice);
        for (int i = 0; i < 100; i++)
        {
            _sut.RecordJobQueue("global");
        }
        
        SystemDateTime.Set(() => _secondSlice);
        for (int i = 0; i < 101; i++)
        {
            _sut.RecordJobQueue("global");
        }
        
        SystemDateTime.Set(() => _thirdSlice);
        for (int i = 0; i < 102; i++)
        {
            _sut.RecordJobQueue("global");
        }
    };

    Because of = async () =>
    {
        SystemDateTime.Set(() => _observedSlice);
        await Rig.Wait(() => _sut.AreWeOkToProcessThisWindow("global"));
    };

    It should_remove_obsolete_entries = () =>
        PAssert.IsTrue(() => _sut.GetWindowsWhichAreWithinLimits().Contains("global"));
    
    Cleanup after = () =>
    {
        _sut?.Dispose();
    };
}