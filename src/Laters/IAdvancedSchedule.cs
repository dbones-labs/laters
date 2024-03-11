﻿namespace Laters;

using Models;

public interface IAdvancedSchedule : ISchedule
{
    void ManyForLater<T>(string name, T jobPayload, string cron, CronOptions options, bool isGlobal);
    
    string ForLaterNext(CronJob cronJob);
}