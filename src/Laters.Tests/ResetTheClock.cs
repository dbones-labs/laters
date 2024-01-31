namespace Laters.Tests;

using Machine.Specifications;

public class ResetTheClock : ICleanupAfterEveryContextInAssembly
{
    public void AfterContextCleanup()
    {
        SystemDateTime.Reset();
    }
}