using System.Collections.Generic;

/// <summary>
/// Static catalog of selectable game modes on the modes list screen.
/// </summary>
public class GameModeDefinition
{
    public string id;
    public string displayName;
    public int requiredPlayers;

    static readonly List<GameModeDefinition> All = new List<GameModeDefinition>
    {
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
            requiredPlayers = 2
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
}
