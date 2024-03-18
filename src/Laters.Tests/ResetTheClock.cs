namespace Laters.Tests;

using Laters.Infrastructure;
using Machine.Specifications;

public class ResetTheClock : ICleanupAfterEveryContextInAssembly
{
    public void AfterContextCleanup()
    {
        SystemDateTime.Reset();
    }
}