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
        ApplyMenuInputState();
        ResetTransientUiInfrastructure();
        MenuSettings.EnsureLoaded();
    }

    /// <summary>
    /// Called from <see cref="VoxelFieldBuilder"/> when the game scene finishes loading.
    /// </summary>
    public static void InitializeGameScene()
    {
        if (GameSession.IsInPrepPhase)
        {
            ApplyMenuInputState();
        }
        else
        {
            ApplyGameInputState();
        }

        ResetTransientUiInfrastructure();
        MenuSettings.EnsureLoaded();
    }

    /// <summary>
    /// Ends the match and replaces the active scene with the main menu.
    /// </summary>
    public static void EnterMainMenu()
    {
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
        ProfileSession.TouchActivity();
        ApplyGameInputState();
        SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// Loads the arena with prep overlay active (match clock starts after prep completes).
    /// </summary>
    public static void EnterGameForPrep()
    {
        ProfileSession.TouchActivity();
        ApplyMenuInputState();
        SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }
}
