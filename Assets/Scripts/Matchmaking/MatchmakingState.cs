/// <summary>
/// Matchmaking phase and snapshot data for UI binding.
/// </summary>
public enum MatchmakingPhase
{
    Idle,
    Searching,
    Found,
    Loading,
    Complete,
    Cancelled
}

public struct MatchmakingSnapshot
{
    public MatchmakingPhase phase;
    public string modeId;
    public string feedLine;
    public float elapsedSeconds;
    public int connectedPlayers;
    public int requiredPlayers;
    public int lastPingMs;
    public bool hasPing;

    public string PlayerCountLabel => $"{connectedPlayers}/{requiredPlayers}";

    public static MatchmakingSnapshot Idle()
    {
        return new MatchmakingSnapshot { phase = MatchmakingPhase.Idle };
    }
}
