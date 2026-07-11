using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small in-game multiplayer status/leave overlay.
/// </summary>
public class MultiplayerSessionHud : MonoBehaviour
{
    static MultiplayerSessionHud _instance;

    Text _statusText;
    Text _joinCodeText;

    public static MultiplayerSessionHud Create()
    {
        if (_instance != null || !MultiplayerSessionManager.IsNetworkSessionActive)
        {
            return _instance;
        }

        GameUICanvas.EnsureExists();
        var layer = GameUICanvas.CreateInteractionLayer("Multiplayer Session HUD", 220);
        var hud = layer.gameObject.AddComponent<MultiplayerSessionHud>();
        hud.Build(layer);
        return hud;
    }

    void Awake()
    {
        _instance = this;
    }

    void OnEnable()
    {
        if (MultiplayerSessionManager.HasInstance)
        {
            MultiplayerSessionManager.Instance.StateChanged += Refresh;
        }
    }

    void OnDisable()
    {
        if (MultiplayerSessionManager.HasInstance)
        {
            MultiplayerSessionManager.Instance.StateChanged -= Refresh;
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    void Build(RectTransform layer)
    {
        var panel = new GameObject("Panel");
        panel.transform.SetParent(layer, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(18f, -18f);
        rect.sizeDelta = new Vector2(340f, 122f);

        var image = panel.AddComponent<Image>();
        image.sprite = MenuUiFactory.WhiteSprite;
        image.color = new Color(0.04f, 0.04f, 0.045f, 0.78f);
        image.raycastTarget = true;

        _statusText = MenuUiFactory.CreateAnchoredText(panel.transform, "Status", string.Empty,
            18, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        var statusRect = _statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(0f, 1f);
        statusRect.offsetMin = new Vector2(14f, -46f);
        statusRect.offsetMax = new Vector2(-14f, -12f);

        _joinCodeText = MenuUiFactory.CreateAnchoredText(panel.transform, "Join Code", string.Empty,
            16, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.86f, 0.9f, 0.96f, 1f));
        var codeRect = _joinCodeText.GetComponent<RectTransform>();
        codeRect.anchorMin = new Vector2(0f, 1f);
        codeRect.anchorMax = new Vector2(1f, 1f);
        codeRect.pivot = new Vector2(0f, 1f);
        codeRect.offsetMin = new Vector2(14f, -78f);
        codeRect.offsetMax = new Vector2(-14f, -46f);

        MenuUiFactory.CreateButton(panel.transform, "Leave Multiplayer", "LEAVE",
            new Vector2(0f, -94f), new Vector2(300f, 34f), () =>
            {
                _ = MultiplayerSessionManager.Instance.LeaveAsync();
            });

        Refresh();
    }

    void Refresh()
    {
        var manager = MultiplayerSessionManager.Instance;
        if (_statusText != null)
        {
            _statusText.text = manager.Status;
        }

        if (_joinCodeText != null)
        {
            _joinCodeText.text = string.IsNullOrEmpty(manager.JoinCode)
                ? string.Empty
                : $"join code: {manager.JoinCode}";
        }
    }
}
