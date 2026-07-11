using System.Collections.Generic;

/// <summary>
/// Static catalog of selectable game modes on the modes list screen.
/// </summary>
public class GameModeDefinition
{
    public enum LoadoutRequirement
    {
        None,
        Slot1,
        Complete
    }

    public string id;
    public string displayName;
    public int requiredPlayers;
    public LoadoutRequirement loadoutRequirement = LoadoutRequirement.Complete;
    public bool skipMatchmakingDelay;
    public bool skipPrepPhase;
    public bool isLocallyPlayable = true;
    public bool requiresOnlineMultiplayer;

    static readonly List<GameModeDefinition> All = new List<GameModeDefinition>
    {
        new GameModeDefinition
        {
            id = "shooting_range",
            displayName = "SHOOTING RANGE",
            requiredPlayers = 1,
            loadoutRequirement = LoadoutRequirement.Slot1,
            skipMatchmakingDelay = true,
            skipPrepPhase = true
        },
        new GameModeDefinition
        {
            id = "test_one_player",
            displayName = "TEST ONE PLAYER",
            requiredPlayers = 1
        },
        new GameModeDefinition
        {
            id = "test_two_player",
            displayName = "TEST TWO PLAYER",
            requiredPlayers = 2,
            requiresOnlineMultiplayer = true
        }
    };

    public static IReadOnlyList<GameModeDefinition> Catalog => All;

    public static GameModeDefinition Get(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].id == id)
            {
                return All[i];
            }
        }

        return null;
    }

    public bool IsPlayable()
    {
        switch (loadoutRequirement)
        {
            case LoadoutRequirement.None:
                return ProfileSession.IsSignedIn;
            case LoadoutRequirement.Slot1:
                return ProfileSession.HasLoadoutSlot1;
            default:
                return ProfileSession.HasCompleteLoadout;
        }
    }
}
