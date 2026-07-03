using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the minimalist main menu (title, Play, Quit) entirely from code
/// so the scene file itself can stay nearly empty.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    static readonly Color Background = new Color(0.97f, 0.97f, 0.97f);
    static readonly Color Ink = new Color(0.08f, 0.08f, 0.08f);

    Font _font;

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
            anchoredPos: new Vector2(0, 200), size: new Vector2(1200, 140));

        CreateText(canvasGo.transform, "Subtitle", "voxel arena prototype",
            fontSize: 30, FontStyle.Normal,
            anchoredPos: new Vector2(0, 110), size: new Vector2(1200, 50));

        CreateButton(canvasGo.transform, "Play Button", "PLAY",
            new Vector2(0, -40), () => SceneManager.LoadScene("Game"));

        CreateButton(canvasGo.transform, "Quit Button", "QUIT",
            new Vector2(0, -140), Quit);
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

        var colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f);
        colors.pressedColor = new Color(0.5f, 0.5f, 0.5f);
        button.colors = colors;

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
