using System.Collections;
using UnityEngine;

/// <summary>
/// Shows the match prep overlay in the game scene after matchmaking completes.
/// </summary>
public class MatchPrepController : MonoBehaviour
{
    MatchClassSelectPanel _panel;
    RectTransform _layer;

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
        GameUICanvas.EnsureExists();
        var layer = GameUICanvas.CreateInteractionLayer("Match Prep", 400);
        _layer = layer;
        transform.SetParent(layer, false);

        _panel = MatchClassSelectPanel.Create(layer);
        _panel.ReadyPressed += HandlePrepReady;
        _panel.Completed += HandlePrepComplete;
        _panel.Show();
        MenuUiFactory.EnsureEventSystem();
        GameUICanvas.BringLayerToFront(_layer);
        SceneFlow.ApplyMenuInputState();
        MatchClockHud.Instance?.SetVisible(false);
        StartCoroutine(RefreshPrepInputState());
    }

    IEnumerator RefreshPrepInputState()
    {
        for (int i = 0; i < 3 && GameSession.IsInPrepPhase && !GameSession.IsPrepReady; i++)
        {
            yield return null;
            MenuUiFactory.EnsureEventSystem();
            GameUICanvas.BringLayerToFront(_layer);
            SceneFlow.ApplyMenuInputState();
        }
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
