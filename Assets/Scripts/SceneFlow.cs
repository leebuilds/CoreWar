using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Single entry point for menu ↔ game scene transitions and shared runtime resets.
/// </summary>
public static class SceneFlow
{
    public const string MainMenuSceneName = "MainMenu";
    public const string GameSceneName = "Game";

    public static bool IsMainMenuActive =>
        SceneManager.GetActiveScene().name == MainMenuSceneName;

    public static bool IsGameActive =>
        SceneManager.GetActiveScene().name == GameSceneName;

    /// <summary>
    /// Free cursor and normal time — used by menu screens and in-match UI overlays.
    /// </summary>
    public static void ApplyMenuInputState()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Locked cursor for first-person gameplay.
    /// </summary>
    public static void ApplyGameInputState()
    {
        if (GamePauseMenu.IsAnyOpen)
        {
            ApplyMenuInputState();
            return;
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Destroys every EventSystem in the loaded scenes so the next bootstrap creates a clean one.
    /// Call only after a scene has finished loading (never from a UI click that still needs the current system).
    /// </summary>
    public static void ResetTransientUiInfrastructure()
    {
        var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);

        foreach (var eventSystem in eventSystems)
        {
            Object.Destroy(eventSystem.gameObject);
        }
    }

    /// <summary>
    /// Called from <see cref="MainMenuController"/> when the menu scene finishes loading.
    /// </summary>
    public static void InitializeMainMenuScene()
    {
        BootTrace.Log("SCENES", "InitializeMainMenuScene");
        ApplyMenuInputState();
        ResetTransientUiInfrastructure();
        MenuUiFactory.EnsureEventSystem();
        MenuSettings.EnsureLoaded();
    }

    /// <summary>
    /// Called from <see cref="VoxelFieldBuilder"/> when the game scene finishes loading.
    /// </summary>
    public static void InitializeGameScene()
    {
        BootTrace.Log(
            "SCENES",
            $"InitializeGameScene prep={GameSession.IsInPrepPhase} mode={GameSession.SelectedGameModeId ?? "null"}");
        if (GameSession.IsInPrepPhase)
        {
            ApplyMenuInputState();
        }
        else
        {
            ApplyGameInputState();
        }

        ResetTransientUiInfrastructure();
        MenuUiFactory.EnsureEventSystem();
        MenuSettings.EnsureLoaded();
    }

    /// <summary>
    /// Ends the match and replaces the active scene with the main menu.
    /// </summary>
    public static void EnterMainMenu()
    {
        BootTrace.Log("SCENES", "EnterMainMenu -> LoadScene(MainMenu)");
        ProfileSession.TouchActivity();
        GameSession.EndMatch();
        ApplyMenuInputState();
        SceneManager.LoadScene(MainMenuSceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// Replaces the active scene with the game field. Match data must already be set on <see cref="GameSession"/>.
    /// </summary>
    public static void EnterGame()
    {
        BootTrace.Log("SCENES", "EnterGame begin");
        GameSession.LogDiagnostics("EnterGame (before guard)");
        if (!GameSession.HasAuthorizedGameEntry)
        {
            BootTrace.LogError(
                "SCENES",
                "EnterGame blocked: no authorized match entry. BeginMatch must run before loading Game.");
            return;
        }

        ProfileSession.TouchActivity();
        ApplyGameInputState();
        GameSession.LogDiagnostics("EnterGame (before LoadScene)");
        LogCanLoadGameScene("EnterGame");
        BootTrace.Log("SCENES", "EnterGame -> LoadScene(Game)");
        SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// Loads the arena with prep overlay active (match clock starts after prep completes).
    /// </summary>
    public static void EnterGameForPrep()
    {
        BootTrace.Log("SCENES", "EnterGameForPrep begin");
        GameSession.LogDiagnostics("EnterGameForPrep (before guard)");
        if (!GameSession.HasAuthorizedGameEntry)
        {
            BootTrace.LogError(
                "SCENES",
                "EnterGameForPrep blocked: no authorized match entry. BeginMatchForPrep must run before loading Game.");
            return;
        }

        ProfileSession.TouchActivity();
        ApplyMenuInputState();
        GameSession.LogDiagnostics("EnterGameForPrep (before LoadScene)");
        LogCanLoadGameScene("EnterGameForPrep");
        BootTrace.Log("SCENES", "EnterGameForPrep -> LoadScene(Game)");
        SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }

    static void LogCanLoadGameScene(string context)
    {
        bool canLoad = Application.CanStreamedLevelBeLoaded(GameSceneName);
        BootTrace.Log("SCENES", $"{context} CanStreamedLevelBeLoaded('{GameSceneName}') = {canLoad}");
        if (!canLoad)
        {
            BootTrace.LogError(
                "SCENES",
                $"Scene '{GameSceneName}' is not loadable in this build. " +
                "Check Build Profile scene list (MainMenu=0, Game=1).");
        }
    }

    /// <summary>
    /// Returns to the main menu when the Game scene loads without an active match
    /// (e.g. opening Game.unity directly in the editor).
    /// </summary>
    public static bool TryBlockUnauthorizedGameScene()
    {
        if (!IsGameActive)
        {
            return false;
        }

        GameSession.RestoreFromLifetimeIfNeeded();
        GameSession.LogDiagnostics("TryBlockUnauthorizedGameScene");

        bool networkActive = MultiplayerSessionManager.IsNetworkSessionActive;
        if (GameSession.HasAuthorizedGameEntry)
        {
            BootTrace.Log(
                "SCENES",
                "Game entry ACCEPTED: menu-started match. " +
                $"mode={GameSession.SelectedGameModeId ?? "null"} " +
                $"IsMatchActive={GameSession.IsMatchActive} networkActive={networkActive}");
            return false;
        }

        BootTrace.LogError(
            "SCENES",
            "Game entry REJECTED -> redirecting to MainMenu. " +
            $"reason={(GameSession.IsMatchActive ? "lifetime object missing (state lost across scene load)" : "no BeginMatch ran (scene opened directly, not via menu)")} " +
            $"mode={GameSession.SelectedGameModeId ?? "null"} " +
            $"IsMatchActive={GameSession.IsMatchActive} networkActive={networkActive}");
        EnterMainMenu();
        return true;
    }
}
