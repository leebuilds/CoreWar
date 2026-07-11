using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main-menu host/join-code panel for two-player online matches.
/// </summary>
public class MultiplayerSessionPanel : MonoBehaviour
{
    Text _statusText;
    Text _joinCodeText;
    Text _errorText;
    Text _helpText;
    InputField _joinCodeInput;

    public static MultiplayerSessionPanel Create(Transform parent, GameModeDefinition mode = null)
    {
        var host = new GameObject("Multiplayer Session Panel");
        host.transform.SetParent(parent, false);
        MenuUiFactory.StretchFull(host.AddComponent<RectTransform>());

        var panel = host.AddComponent<MultiplayerSessionPanel>();
        panel.Build(mode);
        return panel;
    }

    void OnEnable()
    {
        MultiplayerSessionManager.Instance.StateChanged += Refresh;
    }

    void OnDisable()
    {
        if (MultiplayerSessionManager.HasInstance)
        {
            MultiplayerSessionManager.Instance.StateChanged -= Refresh;
        }
    }

    void Build(GameModeDefinition mode)
    {
        string title = mode != null ? mode.displayName : "MULTIPLAYER TEST";
        string footer = mode != null
            ? "host or join · red vs blue teams · sabotage enemy drills"
            : "host or join by code";

        var frame = MenuWindowFrame.CreateScreen(transform, title, showBack: true,
            footer, new Vector2(620f, 660f), showHeader: false, () => Destroy(gameObject));

        _statusText = MenuUiFactory.CreateText(frame.Body, "Status", "offline",
            MenuUiFactory.BodyFontSize, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0f, 198f), new Vector2(520f, 42f));

        _joinCodeText = MenuUiFactory.CreateText(frame.Body, "Join Code", string.Empty,
            MenuUiFactory.BodyFontSize, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0f, 148f), new Vector2(520f, 38f));

        _helpText = MenuUiFactory.CreateText(frame.Body, "Help", string.Empty,
            16, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0f, 102f), new Vector2(520f, 56f));
        _helpText.color = MenuUiFactory.Ink;

        MenuUiFactory.CreateButton(frame.Body, "Host Button", "HOST",
            new Vector2(0f, 34f), MenuUiFactory.StandardButtonSize, () =>
            {
                _ = MultiplayerSessionManager.Instance.HostAsync();
            });

        _joinCodeInput = MenuUiFactory.CreateInputField(frame.Body, "Join Code Input", "join code",
            new Vector2(0f, -48f), MenuUiFactory.StandardInputSize);
        _joinCodeInput.characterLimit = 12;

        MenuUiFactory.CreateButton(frame.Body, "Join Button", "JOIN",
            new Vector2(0f, -128f), MenuUiFactory.StandardButtonSize, () =>
            {
                _ = MultiplayerSessionManager.Instance.JoinAsync(_joinCodeInput.text);
            });

        MenuUiFactory.CreateButton(frame.Body, "Leave Button", "DISCONNECT",
            new Vector2(0f, -208f), MenuUiFactory.StandardButtonSize, () =>
            {
                _ = MultiplayerSessionManager.Instance.LeaveAsync();
            });

        _errorText = MenuUiFactory.CreateText(frame.Body, "Error", string.Empty,
            18, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, -271f), new Vector2(520f, 72f));
        _errorText.color = new Color(0.86f, 0.12f, 0.1f, 1f);

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
                ? "join code appears here"
                : $"join code: {manager.JoinCode}";
        }

        if (_helpText != null)
        {
            _helpText.text = manager.IsBusy
                ? "initializing services..."
                : "hold T on enemy drills to sabotage · hold T on your drill to restart it";
        }

        if (_errorText != null)
        {
            _errorText.text = manager.Error;
        }
    }
}
