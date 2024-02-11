namespace Laters.Tests;

using Infrastucture;
using Machine.Specifications;

public class ResetTheClock : ICleanupAfterEveryContextInAssembly
{
    public void AfterContextCleanup()
    {
        SystemDateTime.Reset();
    }
}