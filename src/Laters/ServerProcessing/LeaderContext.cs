namespace Laters.ServerProcessing;

using Models;

public class LeaderContext
{
    public Leader? Leader { get; set; }
    public string ServerId { get; set; } = Guid.NewGuid().ToString("D");

    public bool IsLeader => ServerId.Equals(Leader?.ServerId);

    public bool IsThisServer(Leader currentLeader)
    {
        return ServerId.Equals(currentLeader?.ServerId);
    }
}