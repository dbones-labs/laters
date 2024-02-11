namespace Laters.Data;

using Exceptions;

/// <summary>
/// when a concurrency happens, we run logic on this mainly
/// around leader election.
/// </summary>
public class ConcurrencyException : LatersException
{
    public ConcurrencyException(Exception currencyException)
        : base("Concurrency issue", currencyException) { }
}