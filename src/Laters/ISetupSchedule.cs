namespace Laters;

/// <summary>
/// setup a Global set of cron jobs.
/// items added via this will be treated as the complete set of global cron jobs
/// </summary>
public interface ISetupSchedule
{
    void Configure(IScheduleCron scheduleCron);
}