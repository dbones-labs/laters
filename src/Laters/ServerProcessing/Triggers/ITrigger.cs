namespace Laters;

/// <summary>
/// this is used to pause a running thread, until a condition is met
/// </summary>
public interface ITrigger
{
    Task Wait(CancellationToken cancellationToken);
}