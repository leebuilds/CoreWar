using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom matchmaking panel with live feed, timer, player count, and cancel.
/// </summary>
public class MatchmakingPanel : MonoBehaviour
{
    Text _feedText;
    Text _timerText;
    Text _countText;
    GameObject _root;
    GameObject _visualRoot;
    Action _onCancel;
    Action _onSettings;

    public static MatchmakingPanel Create(Transform parent, Action onCancel, Action onSettings)
    {
        var host = new GameObject("Matchmaking Panel Host");
        host.transform.SetParent(parent, false);
        MenuUiFactory.StretchFull(host.AddComponent<RectTransform>());

        var panel = host.AddComponent<MatchmakingPanel>();
        panel._onCancel = onCancel;
        panel._onSettings = onSettings;
        panel.Build();
        MatchmakingSession.Changed += panel.HandleSnapshotChanged;
        MenuSettings.Changed += panel.HandleThemeChanged;
        host.SetActive(false);
        return panel;
    }

    void Build()
    {
        _visualRoot = new GameObject("Theme Visuals");
        _visualRoot.transform.SetParent(transform, false);
        MenuUiFactory.StretchFull(_visualRoot.AddComponent<RectTransform>());

        var blocker = new GameObject("Input Blocker");
        blocker.transform.SetParent(_visualRoot.transform, false);
        MenuUiFactory.StretchFull(blocker.AddComponent<RectTransform>());
        var blockerImage = blocker.AddComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0.12f);
        blockerImage.raycastTarget = true;

        _root = new GameObject("Panel Root");
        _root.transform.SetParent(_visualRoot.transform, false);
        var rootRect = _root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.sizeDelta = new Vector2(360f, 360f);
        rootRect.anchoredPosition = new Vector2(0f, 48f);

        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(_root.transform, false);
        MenuUiFactory.StretchFull(borderGo.AddComponent<RectTransform>());
        borderGo.AddComponent<Image>().color = MenuUiFactory.Ink;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(_root.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        fillGo.AddComponent<Image>().color = MenuUiFactory.PanelFill;

        MenuUiFactory.BuildMilitaryTitleBar(fillGo.transform, 0f, "MATCHMAKING", showBack: false, null, out _);

        _feedText = MenuUiFactory.CreateAnchoredText(fillGo.transform, "Feed", "searching for players",
            MenuUiFactory.BodyFontSize, FontStyle.Normal, TextAnchor.UpperCenter, MenuUiFactory.MutedInk);
        var feedRect = _feedText.GetComponent<RectTransform>();
        feedRect.anchorMin = new Vector2(0.08f, 0.52f);
        feedRect.anchorMax = new Vector2(0.92f, 0.72f);
        feedRect.offsetMin = Vector2.zero;
        feedRect.offsetMax = Vector2.zero;

        _timerText = MenuUiFactory.CreateAnchoredText(fillGo.transform, "Timer", "0:00",
            MenuUiFactory.TitleFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, MenuUiFactory.Ink);
        var timerRect = _timerText.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.08f, 0.30f);
        timerRect.anchorMax = new Vector2(0.45f, 0.48f);
        timerRect.offsetMin = Vector2.zero;
        timerRect.offsetMax = Vector2.zero;

        _countText = MenuUiFactory.CreateAnchoredText(fillGo.transform, "Count", "0/1",
            MenuUiFactory.TitleFontSize, FontStyle.Bold, TextAnchor.MiddleRight, MenuUiFactory.Ink);
        var countRect = _countText.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0.55f, 0.30f);
        countRect.anchorMax = new Vector2(0.92f, 0.48f);
        countRect.offsetMin = Vector2.zero;
        countRect.offsetMax = Vector2.zero;

        MenuUiFactory.CreateButton(fillGo.transform, "Settings", "SETTINGS",
            new Vector2(0f, -92f), new Vector2(280f, MenuUiFactory.CompactControlHeight),
            () => _onSettings?.Invoke());

        MenuUiFactory.CreateButton(fillGo.transform, "Cancel Matchmaking", "CANCEL MATCHMAKING",
            new Vector2(0f, -142f), new Vector2(280f, MenuUiFactory.CompactControlHeight),
            () => _onCancel?.Invoke());
    }

    void OnDestroy()
    {
        MatchmakingSession.Changed -= HandleSnapshotChanged;
        MenuSettings.Changed -= HandleThemeChanged;
    }

    void HandleThemeChanged()
    {
        if (_visualRoot != null)
        {
            Destroy(_visualRoot);
        }

        Build();
        ApplySnapshot(MatchmakingSession.Snapshot);
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy || _timerText == null)
        {
            return;
        }

        _timerText.text = FormatElapsed(MatchmakingSession.ElapsedSeconds);
    }

    public void Show()
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        ApplySnapshot(MatchmakingSession.Snapshot);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void HandleSnapshotChanged(MatchmakingSnapshot snapshot)
    {
        ApplySnapshot(snapshot);
    }

    void ApplySnapshot(MatchmakingSnapshot snapshot)
    {
        if (_feedText != null)
        {
            _feedText.text = snapshot.feedLine ?? string.Empty;
        }

        if (_timerText != null)
        {
            _timerText.text = FormatElapsed(snapshot.elapsedSeconds);
        }

        if (_countText != null)
        {
            _countText.text = snapshot.PlayerCountLabel;
        }
    }

    static string FormatElapsed(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = total / 60;
        int secs = total % 60;
        return $"{minutes}:{secs:00}";
    }
}
