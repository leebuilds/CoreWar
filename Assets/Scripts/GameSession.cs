using UnityEngine;

/// <summary>
/// Carries menu choices into the Game scene.
/// </summary>
public static class GameSession
{
    public enum Team
    {
        Red,
        Blue,
        Yellow,
        Green
    }

    public static Team SelectedTeam { get; private set; } = Team.Red;
    public static int JerseyNumber { get; private set; } = 7;
    public static bool HasTeamSelected { get; private set; }
    public static bool HasLoadoutSelected { get; private set; }

    public static string LoadoutCardIdA { get; private set; }
    public static string LoadoutCardIdB { get; private set; }
    public static string ActiveCardId { get; private set; }
    public static CardKitDefinition ActiveKit { get; private set; }

    public static Color TeamColor(Team team)
    {
        switch (team)
        {
            case Team.Blue: return new Color(0.22f, 0.45f, 0.95f);
            case Team.Yellow: return new Color(0.95f, 0.82f, 0.18f);
            case Team.Green: return new Color(0.22f, 0.78f, 0.38f);
            default: return new Color(0.92f, 0.22f, 0.24f);
        }
    }

    public static void BeginMatch(Team team)
    {
        BeginMatch(team, null, null, null);
    }

    public static void BeginMatch(Team team, string loadoutA, string loadoutB, string initialActiveCardId)
    {
        SelectedTeam = team;
        JerseyNumber = Random.Range(1, 100);
        HasTeamSelected = true;

        LoadoutCardIdA = loadoutA;
        LoadoutCardIdB = loadoutB;
        HasLoadoutSelected = !string.IsNullOrEmpty(loadoutA) && !string.IsNullOrEmpty(loadoutB);

        if (HasLoadoutSelected)
        {
            SetActiveCard(initialActiveCardId ?? loadoutA);
        }
        else
        {
            ActiveCardId = null;
            ActiveKit = CardKitDefinition.DefaultInfantryPlaceholder();
        }
    }

    public static void SetActiveCard(string cardId)
    {
        ActiveCardId = cardId;
        var card = CardCatalog.Get(cardId);
        ActiveKit = card?.kit ?? CardKitDefinition.DefaultInfantryPlaceholder();
    }

    public static void EndMatch()
    {
        HasTeamSelected = false;
        HasLoadoutSelected = false;
        LoadoutCardIdA = null;
        LoadoutCardIdB = null;
        ActiveCardId = null;
        ActiveKit = null;
    }

    public static CardDefinition GetActiveCardDefinition()
    {
        return CardCatalog.Get(ActiveCardId);
    }
}
