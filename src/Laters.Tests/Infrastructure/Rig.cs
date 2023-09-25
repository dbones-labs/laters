namespace Laters.Tests.Infrastructure;

public static class Rig
{
    public static Task Wait(Func<bool> predicate, TimeSpan timeSpan)
    {
        DateTime failedAt = DateTime.UtcNow.Add(timeSpan);
        while (!predicate())
        {
            if (DateTime.UtcNow > failedAt) throw new TimeoutException("took too long");
            Task.Delay(50);
        }

        return Task.CompletedTask;
    }
    
    public static async Task Wait(Func<bool> predicate)
    {
        await Wait(predicate, TimeSpan.FromSeconds(200));
    }
}