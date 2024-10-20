﻿namespace Laters.Tests.Infrastructure;

using System.Linq.Expressions;
using PowerAssert;

public static class Rig
{
    public static async Task Wait(Expression<Func<bool>> predicate, TimeSpan timeSpan)
    {
        var compiledPredicate = predicate.Compile();
        var failedAt = DateTime.UtcNow.Add(timeSpan);
        while (!compiledPredicate())
        {
            if (DateTime.UtcNow > failedAt)
            {
                Console.WriteLine("took to long");
                PAssert.IsTrue(predicate); // we want it to print out the values
            }
            await Task.Delay(50);
        }
    }
    
    public static async Task Wait(Expression<Func<bool>> predicate)
    {
        await Wait(predicate, TimeSpan.FromSeconds(30));
    }
    
    
    public static async Task<Exception?> TryWait(Expression<Func<bool>> predicate, TimeSpan timeSpan)
    {
        try
        {
            await Wait(predicate, timeSpan);
            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }
    
    public static async Task<Exception?> TryWait(Expression<Func<bool>> predicate)
    {
        var exception = await TryWait(predicate, TimeSpan.FromSeconds(30));
        return exception;
    }
}