using UnityEngine;

/// <summary>
/// Shows the match prep overlay in the game scene after matchmaking completes.
/// </summary>
public class MatchPrepController : MonoBehaviour
{
    MatchClassSelectPanel _panel;

    public static MatchPrepController Create()
    {
        if (!GameSession.IsInPrepPhase)
        {
            return null;
        }

        var go = new GameObject("Match Prep Controller");
        var controller = go.AddComponent<MatchPrepController>();
        controller.Initialize();
        return controller;
    }

    void Initialize()
    {
        MenuUiFactory.EnsureEventSystem();

        RectTransform root;
        MenuUiFactory.CreateCanvas("Prep Canvas", out root);
        var canvas = root.GetComponent<Canvas>();
        canvas.sortingOrder = 200;

        _panel = MatchClassSelectPanel.Create(root);
        _panel.ReadyPressed += HandlePrepReady;
        _panel.Completed += HandlePrepComplete;
        _panel.Show();
        SceneFlow.ApplyMenuInputState();
        MatchClockHud.Instance?.SetVisible(false);
    }

    void HandlePrepReady(int spawnSlotIndex)
    {
        SceneFlow.ApplyGameInputState();
    }

    void HandlePrepComplete(int spawnSlotIndex)
    {
        var profile = ProfileSession.ActiveProfile;
        if (profile?.loadoutCardIds == null || spawnSlotIndex < 0 || spawnSlotIndex >= profile.loadoutCardIds.Length)
        {
            return;
        }

        GameSession.CompletePrep(profile.loadoutCardIds[spawnSlotIndex]);
        MenuUiSounds.PlayGunshot();
        _panel?.Hide();
        SceneFlow.ApplyGameInputState();
        MatchClockHud.Instance?.SetVisible(!GamePauseMenu.IsAnyOpen);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (_panel != null)
        {
            _panel.ReadyPressed -= HandlePrepReady;
            _panel.Completed -= HandlePrepComplete;
        }
    }
}
