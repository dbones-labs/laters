namespace Laters.ServerProcessing;

using ClientProcessing;

public interface IWorkerClient
{
    Task DelegateJob(ProcessJob processJob);
}