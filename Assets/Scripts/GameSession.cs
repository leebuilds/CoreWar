using System;
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
    public static bool IsMatchActive { get; private set; }
    public static bool IsInPrepPhase { get; private set; }
    public static bool IsPrepReady { get; private set; }
    public static bool HasLoadoutSelected { get; private set; }

    public static string SelectedGameModeId { get; private set; }
    public static int RequiredPlayers { get; private set; } = 1;

    public static string LoadoutCardIdA { get; private set; }
    public static string LoadoutCardIdB { get; private set; }
    public static string ActiveCardId { get; private set; }
    public static CardKitDefinition ActiveKit { get; private set; }

    static double _matchStartUtcSeconds;

    public static Color TeamColor(Team team)
    {
        switch (team)
        {
            case Team.Blue: return new Color(0.18f, 0.36f, 0.78f);
            case Team.Yellow: return new Color(0.78f, 0.66f, 0.14f);
            case Team.Green: return new Color(0.18f, 0.62f, 0.30f);
            default: return new Color(0.78f, 0.18f, 0.20f);
        }
    }

    public static void BeginMatch(Team team)
    {
        BeginMatch(team, null, null, null);
    }

    public static void BeginMatch(Team team, string loadoutA, string loadoutB, string initialActiveCardId)
    {
        BeginMatch(team, loadoutA, loadoutB, initialActiveCardId, SelectedGameModeId, RequiredPlayers);
    }

    public static void BeginMatch(Team team, string loadoutA, string loadoutB, string initialActiveCardId,
        string gameModeId, int requiredPlayers)
    {
        BeginMatch(team, loadoutA, loadoutB, initialActiveCardId, gameModeId, requiredPlayers, inPrepPhase: false);
    }

    public static void BeginMatchForPrep(Team team, string loadoutA, string loadoutB, string initialActiveCardId,
        string gameModeId, int requiredPlayers)
    {
        BeginMatch(team, loadoutA, loadoutB, initialActiveCardId, gameModeId, requiredPlayers, inPrepPhase: true);
    }

    static void BeginMatch(Team team, string loadoutA, string loadoutB, string initialActiveCardId,
        string gameModeId, int requiredPlayers, bool inPrepPhase)
    {
        SelectedTeam = team;
        JerseyNumber = UnityEngine.Random.Range(1, 100);
        IsMatchActive = true;
        IsInPrepPhase = inPrepPhase;
        IsPrepReady = false;
        SelectedGameModeId = gameModeId;
        RequiredPlayers = Mathf.Max(1, requiredPlayers);
        _matchStartUtcSeconds = 0d;

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

    public static void MarkPrepReady(string activeCardId)
    {
        if (!IsInPrepPhase || IsPrepReady)
        {
            return;
        }

        if (!string.IsNullOrEmpty(activeCardId))
        {
            SetActiveCard(activeCardId);
        }

        IsPrepReady = true;
    }

    public static void CompletePrep(string activeCardId)
    {
        if (!IsInPrepPhase)
        {
            return;
        }

        if (!string.IsNullOrEmpty(activeCardId))
        {
            SetActiveCard(activeCardId);
        }

        IsPrepReady = false;
        IsInPrepPhase = false;
        MarkMatchStarted();
    }

    public static void MarkMatchStarted()
    {
        _matchStartUtcSeconds = GetUtcNowSeconds();
    }

    public static void EnsureMatchClockStarted()
    {
        if (IsMatchActive && _matchStartUtcSeconds <= 0d)
        {
            MarkMatchStarted();
        }
    }

    public static float MatchElapsedSeconds
    {
        get
        {
            if (!IsMatchActive || _matchStartUtcSeconds <= 0d)
            {
                return 0f;
            }

            return (float)(GetUtcNowSeconds() - _matchStartUtcSeconds);
        }
    }

    public static string FormatMatchElapsedClock()
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(MatchElapsedSeconds));
        int minutes = total / 60;
        int seconds = total % 60;
        return $"{minutes}:{seconds:00}";
    }

    public static void SetActiveCard(string cardId)
    {
        ActiveCardId = cardId;
        var card = CardCatalog.Get(cardId);
        ActiveKit = card?.kit ?? CardKitDefinition.DefaultInfantryPlaceholder();
    }

    public static void EndMatch()
    {
        IsMatchActive = false;
        IsInPrepPhase = false;
        IsPrepReady = false;
        HasLoadoutSelected = false;
        SelectedGameModeId = null;
        RequiredPlayers = 1;
        LoadoutCardIdA = null;
        LoadoutCardIdB = null;
        ActiveCardId = null;
        ActiveKit = null;
        _matchStartUtcSeconds = 0d;
    }

    static double GetUtcNowSeconds()
    {
        return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}
