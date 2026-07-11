using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Temporary end-of-match result modal for scripted test matches.
/// </summary>
public class TestMatchResultPanel : MonoBehaviour
{
    static TestMatchResultPanel _instance;

    public static TestMatchResultPanel Create(bool won)
    {
        if (_instance != null)
        {
            Destroy(_instance.gameObject);
            _instance = null;
        }

        GameUICanvas.EnsureExists();
        var layer = GameUICanvas.CreateInteractionLayer("Match Result", 600);
        var panel = layer.gameObject.AddComponent<TestMatchResultPanel>();
        panel.Build(layer, won);
        SceneFlow.ApplyMenuInputState();
        return panel;
    }

    void Awake()
    {
        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    void Build(RectTransform layer, bool won)
    {
        MenuUiFactory.CreateFullscreenDim(layer, 0.45f);

        string result = won ? "YOU WON" : "YOU LOST";
        var frame = MenuWindowFrame.CreateScreen(
            layer,
            result,
            showBack: false,
            footerText: "match complete",
            size: new Vector2(520f, 320f),
            showHeader: false,
            onBack: null,
            animateFade: true);

        var resultText = MenuUiFactory.CreateAnchoredText(
            frame.Body,
            "Result Text",
            result,
            44,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            MenuUiFactory.Ink);
        var resultRect = resultText.GetComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0f, 0.45f);
        resultRect.anchorMax = new Vector2(1f, 0.95f);
        resultRect.offsetMin = Vector2.zero;
        resultRect.offsetMax = Vector2.zero;

        MenuUiFactory.CreateButton(
            frame.Body,
            "Continue Button",
            "CONTINUE",
            new Vector2(0f, -54f),
            MenuUiFactory.StandardButtonSize,
            SceneFlow.EnterMainMenu);
    }
}
