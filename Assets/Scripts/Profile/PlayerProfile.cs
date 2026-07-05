using System;

/// <summary>
/// Persisted player account data. Swap storage via IProfileRepository for online play.
/// </summary>
[Serializable]
public class PlayerProfile
{
    public string profileId;
    public string username;
    public string passcodeHash;
    public string passcodeSalt;
    public string[] ownedCardIds;
    public string[] loadoutCardIds;
    public string lastActiveUtc;

    public bool HasCompleteLoadout =>
        loadoutCardIds != null &&
        loadoutCardIds.Length >= 2 &&
        !string.IsNullOrEmpty(loadoutCardIds[0]) &&
        !string.IsNullOrEmpty(loadoutCardIds[1]);

    public static PlayerProfile CreateNew(string username, string passcodeHash, string passcodeSalt, string[] allCardIds)
    {
        return new PlayerProfile
        {
            profileId = Guid.NewGuid().ToString("N"),
            username = username,
            passcodeHash = passcodeHash,
            passcodeSalt = passcodeSalt,
            ownedCardIds = allCardIds,
            loadoutCardIds = new[] { string.Empty, string.Empty },
            lastActiveUtc = DateTime.UtcNow.ToString("o")
        };
    }

    public DateTime GetLastActiveUtc()
    {
        if (string.IsNullOrEmpty(lastActiveUtc))
        {
            return DateTime.MinValue;
        }

        return DateTime.TryParse(lastActiveUtc, out var parsed) ? parsed : DateTime.MinValue;
    }

    public void TouchLastActive()
    {
        lastActiveUtc = DateTime.UtcNow.ToString("o");
    }
}
