namespace Laters;

public class ConcurrencyException : LatersException
{
    public ConcurrencyException(Exception currencyException)
        : base("Concurrency issue", currencyException) { }
}