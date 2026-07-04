using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Minimalist main menu with team selection before entering the game.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    static readonly Color Background = new Color(0.97f, 0.97f, 0.97f);
    static readonly Color Ink = new Color(0.08f, 0.08f, 0.08f);

    Font _font;
    GameSession.Team _selectedTeam = GameSession.Team.Red;
    readonly System.Collections.Generic.Dictionary<GameSession.Team, Image> _teamButtons =
        new System.Collections.Generic.Dictionary<GameSession.Team, Image>();
    Image _playButtonImage;
    Text _playButtonLabel;

    void Awake()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateCamera();
        CreateEventSystem();
        CreateMenu();
    }

    void CreateCamera()
    {
        var camGo = new GameObject("Menu Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Background;
        cam.cullingMask = 0;
    }

    void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    void CreateMenu()
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        CreateText(canvasGo.transform, "Title", "COREWAR",
            fontSize: 110, FontStyle.Bold,
            anchoredPos: new Vector2(0, 220), size: new Vector2(1200, 140));

        CreateText(canvasGo.transform, "Subtitle", "pick your team",
            fontSize: 30, FontStyle.Normal,
            anchoredPos: new Vector2(0, 130), size: new Vector2(1200, 50));

        CreateTeamButtons(canvasGo.transform);
        CreatePlayButton(canvasGo.transform);
        CreateButton(canvasGo.transform, "Quit Button", "QUIT",
            new Vector2(0, -210), Quit);
    }

    void CreateTeamButtons(Transform parent)
    {
        CreateText(parent, "Team Label", "TEAM",
            fontSize: 22, FontStyle.Bold,
            anchoredPos: new Vector2(0, 55), size: new Vector2(400, 40));

        var teams = new[]
        {
            (GameSession.Team.Red, "RED"),
            (GameSession.Team.Blue, "BLUE"),
            (GameSession.Team.Yellow, "YELLOW"),
            (GameSession.Team.Green, "GREEN")
        };

        float startX = -420f;
        for (int i = 0; i < teams.Length; i++)
        {
            var team = teams[i].Item1;
            var label = teams[i].Item2;
            float x = startX + i * 280f;
            CreateTeamButton(parent, label, new Vector2(x, -20), team);
        }
    }

    void CreateTeamButton(Transform parent, string label, Vector2 anchoredPos, GameSession.Team team)
    {
        var go = new GameObject($"{label} Team Button");
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = GameSession.TeamColor(team);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => SelectTeam(team));
        _teamButtons[team] = image;

        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(240, 64);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var text = labelGo.AddComponent<Text>();
        text.font = _font;
        text.text = label;
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.color = Ink;
        text.alignment = TextAnchor.MiddleCenter;

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        if (team == _selectedTeam)
        {
            HighlightTeam(team);
        }
    }

    void SelectTeam(GameSession.Team team)
    {
        _selectedTeam = team;
        HighlightTeam(team);
    }

    void HighlightTeam(GameSession.Team team)
    {
        foreach (var pair in _teamButtons)
        {
            pair.Value.color = pair.Key == team
                ? Color.Lerp(GameSession.TeamColor(pair.Key), Color.white, 0.18f)
                : GameSession.TeamColor(pair.Key);
        }

        if (_playButtonLabel != null)
        {
            _playButtonLabel.text = $"PLAY AS {team.ToString().ToUpper()}";
        }
    }

    void CreatePlayButton(Transform parent)
    {
        var go = new GameObject("Play Button");
        go.transform.SetParent(parent, false);

        _playButtonImage = go.AddComponent<Image>();
        _playButtonImage.color = Ink;

        var button = go.AddComponent<Button>();
        button.targetGraphic = _playButtonImage;
        button.onClick.AddListener(StartGame);

        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, -110);
        rect.sizeDelta = new Vector2(360, 72);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        _playButtonLabel = labelGo.AddComponent<Text>();
        _playButtonLabel.font = _font;
        _playButtonLabel.text = $"PLAY AS {_selectedTeam.ToString().ToUpper()}";
        _playButtonLabel.fontSize = 30;
        _playButtonLabel.fontStyle = FontStyle.Bold;
        _playButtonLabel.color = Background;
        _playButtonLabel.alignment = TextAnchor.MiddleCenter;

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    void StartGame()
    {
        GameSession.BeginMatch(_selectedTeam);
        SceneManager.LoadScene("Game");
    }

    void CreateText(Transform parent, string name, string content,
        int fontSize, FontStyle style, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.font = _font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Ink;
        text.alignment = TextAnchor.MiddleCenter;

        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }

    void CreateButton(Transform parent, string name, string label,
        Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = Ink;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(320, 72);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);

        var text = labelGo.AddComponent<Text>();
        text.font = _font;
        text.text = label;
        text.fontSize = 34;
        text.fontStyle = FontStyle.Bold;
        text.color = Background;
        text.alignment = TextAnchor.MiddleCenter;

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
