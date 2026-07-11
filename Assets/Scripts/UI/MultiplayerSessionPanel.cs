using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main-menu host/join-code test panel.
/// </summary>
public class MultiplayerSessionPanel : MonoBehaviour
{
    Text _statusText;
    Text _joinCodeText;
    Text _errorText;
    InputField _joinCodeInput;

    public static MultiplayerSessionPanel Create(Transform parent)
    {
        var host = new GameObject("Multiplayer Session Panel");
        host.transform.SetParent(parent, false);
        MenuUiFactory.StretchFull(host.AddComponent<RectTransform>());

        var panel = host.AddComponent<MultiplayerSessionPanel>();
        panel.Build();
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

    void Build()
    {
        var frame = MenuWindowFrame.CreateScreen(transform, "MULTIPLAYER TEST", showBack: true,
            "host or join by code", new Vector2(620f, 620f), showHeader: false, () => Destroy(gameObject));

        _statusText = MenuUiFactory.CreateText(frame.Body, "Status", "offline",
            MenuUiFactory.BodyFontSize, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0f, 168f), new Vector2(520f, 42f));

        _joinCodeText = MenuUiFactory.CreateText(frame.Body, "Join Code", string.Empty,
            MenuUiFactory.BodyFontSize, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0f, 118f), new Vector2(520f, 38f));

        MenuUiFactory.CreateButton(frame.Body, "Host Button", "HOST",
            new Vector2(0f, 50f), MenuUiFactory.StandardButtonSize, () =>
            {
                _ = MultiplayerSessionManager.Instance.HostAsync();
            });

        _joinCodeInput = MenuUiFactory.CreateInputField(frame.Body, "Join Code Input", "join code",
            new Vector2(0f, -32f), MenuUiFactory.StandardInputSize);
        _joinCodeInput.characterLimit = 12;

        MenuUiFactory.CreateButton(frame.Body, "Join Button", "JOIN",
            new Vector2(0f, -112f), MenuUiFactory.StandardButtonSize, () =>
            {
                _ = MultiplayerSessionManager.Instance.JoinAsync(_joinCodeInput.text);
            });

        MenuUiFactory.CreateButton(frame.Body, "Leave Button", "DISCONNECT",
            new Vector2(0f, -192f), MenuUiFactory.StandardButtonSize, () =>
            {
                _ = MultiplayerSessionManager.Instance.LeaveAsync();
            });

        _errorText = MenuUiFactory.CreateText(frame.Body, "Error", string.Empty,
            18, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, -255f), new Vector2(520f, 72f));
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

        if (_errorText != null)
        {
            _errorText.text = manager.Error;
        }
    }
}
