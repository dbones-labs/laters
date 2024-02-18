namespace Laters.Tests.ServerSide.Windows;

using Infrastructure;
using Infrastucture;
using Machine.Specifications;
using PowerAssert;
using ServerProcessing.Windows;

[Subject("window")]
[Tags("class-test")]
class When_entries_are_outside_of_the_window
{
    static Window? _sut;

    static DateTime _firstSlice = new(1999, 1, 1, 1, 0, 0);
    static DateTime _secondSlice = new(1999, 1, 1, 1, 0, 5);
    static DateTime _thirdSlice = new(1999, 1, 1, 1, 0, 10);
    static DateTime _observedSlice = new(1999, 1, 1, 1, 0, 15);

    Establish context = () =>
    {
        _sut = new Window();
        _sut.CleanUpInterval = TimeSpan.FromMilliseconds(10);
        _sut.SlicePrecision = TimeSpan.FromSeconds(1);
        _sut.Span = TimeSpan.FromSeconds(11);
        _sut.MaxCount = 1000;
        _sut.Initialize(new CancellationToken());

        SystemDateTime.Set(() => _firstSlice);
        _sut.AddItemsToWindow(SystemDateTime.UtcNow, 100);
        
        SystemDateTime.Set(() => _secondSlice);
        _sut.AddItemsToWindow(SystemDateTime.UtcNow, 101);
        
        SystemDateTime.Set(() => _thirdSlice);
        _sut.AddItemsToWindow(SystemDateTime.UtcNow, 102);
    };

    Because of = async () =>
    {
        SystemDateTime.Set(() => _observedSlice);
        await Rig.Wait(() => _sut.Count == 203);
    };

    It should_remove_obsolete_entries = () =>
        PAssert.IsTrue(() => _sut.Count == 203 );
    
    Cleanup after = () =>
    {
        _sut?.Dispose();
    };
}