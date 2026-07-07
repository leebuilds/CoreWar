using System;
using System.Globalization;

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
    public int profileDataVersion;

    public bool HasLoadoutSlot1 =>
        loadoutCardIds != null &&
        loadoutCardIds.Length >= 1 &&
        !string.IsNullOrEmpty(loadoutCardIds[0]);

    public bool HasCompleteLoadout =>
        loadoutCardIds != null &&
        loadoutCardIds.Length >= 2 &&
        !string.IsNullOrEmpty(loadoutCardIds[0]) &&
        !string.IsNullOrEmpty(loadoutCardIds[1]);

    public static PlayerProfile CreateNew(string username, string passcodeHash, string passcodeSalt, string[] ownedCardIds)
    {
        return new PlayerProfile
        {
            profileId = Guid.NewGuid().ToString("N"),
            username = username,
            passcodeHash = passcodeHash,
            passcodeSalt = passcodeSalt,
            ownedCardIds = ownedCardIds,
            loadoutCardIds = new[] { string.Empty, string.Empty },
            lastActiveUtc = DateTime.UtcNow.ToString("o"),
            profileDataVersion = ProfileSession.CurrentProfileDataVersion
        };
    }

    public DateTime GetLastActiveUtc()
    {
        if (string.IsNullOrEmpty(lastActiveUtc))
        {
            return DateTime.UtcNow;
        }

        if (DateTime.TryParse(
                lastActiveUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            return parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : parsed.ToUniversalTime();
        }

        if (DateTime.TryParse(lastActiveUtc, out parsed))
        {
            return parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : parsed.ToUniversalTime();
        }

        return DateTime.UtcNow;
    }

    public void TouchLastActive()
    {
        lastActiveUtc = DateTime.UtcNow.ToString("o");
    }
}
