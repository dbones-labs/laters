namespace Laters.Configuration;

using Exceptions;

public class MissingStorageConfigurationException : LatersException
{
    public MissingStorageConfigurationException() : base("no storage has been setup")
    {
    }
}