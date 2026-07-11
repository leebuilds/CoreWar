using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Upper-left objective progress bar for Test Map 1.
/// </summary>
public class TestObjectiveHud : MonoBehaviour
{
    static TestObjectiveHud _instance;

    RectTransform _root;
    RectTransform _fill;
    Text _label;
    Text _interactionText;

    public static TestObjectiveHud Create()
    {
        if (_instance != null)
        {
            return _instance;
        }

        GameUICanvas.EnsureExists();
        var layer = GameUICanvas.CreateLayer("Objective HUD");
        var host = layer.gameObject.AddComponent<TestObjectiveHud>();
        host.Build(layer);
        return host;
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

    void Build(RectTransform layer)
    {
        var rootGo = new GameObject("Objective Bar");
        rootGo.transform.SetParent(layer, false);
        _root = rootGo.AddComponent<RectTransform>();
        _root.anchorMin = new Vector2(0f, 1f);
        _root.anchorMax = new Vector2(0f, 1f);
        _root.pivot = new Vector2(0f, 1f);
        _root.anchoredPosition = new Vector2(16f, -16f);
        _root.sizeDelta = new Vector2(300f, 38f);

        var background = rootGo.AddComponent<Image>();
        background.sprite = MenuUiFactory.WhiteSprite;
        background.color = new Color(0.42f, 0.42f, 0.42f, 0.94f);
        background.raycastTarget = false;

        CreateBorder(rootGo.transform);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(rootGo.transform, false);
        _fill = fillGo.AddComponent<RectTransform>();
        _fill.anchorMin = new Vector2(0f, 0f);
        _fill.anchorMax = new Vector2(0f, 1f);
        _fill.pivot = new Vector2(0f, 0.5f);
        _fill.anchoredPosition = new Vector2(3f, 0f);
        _fill.sizeDelta = new Vector2(0f, -6f);

        var fillImage = fillGo.AddComponent<Image>();
        fillImage.sprite = MenuUiFactory.WhiteSprite;
        fillImage.color = new Color(0.16f, 0.78f, 0.26f, 1f);
        fillImage.raycastTarget = false;

        _label = MenuUiFactory.CreateAnchoredText(
            rootGo.transform,
            "Label",
            "TEST MAP 1",
            16,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            Color.white);
        MenuUiFactory.StretchFull(_label.GetComponent<RectTransform>());

        _interactionText = MenuUiFactory.CreateAnchoredText(
            rootGo.transform,
            "Interaction",
            string.Empty,
            14,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            Color.black);
        var interactionRect = _interactionText.GetComponent<RectTransform>();
        interactionRect.anchorMin = new Vector2(0f, 0f);
        interactionRect.anchorMax = new Vector2(1f, 0f);
        interactionRect.pivot = new Vector2(0f, 1f);
        interactionRect.anchoredPosition = new Vector2(0f, -6f);
        interactionRect.sizeDelta = new Vector2(0f, 24f);
    }

    void LateUpdate()
    {
        var objective = TestMapObjectiveManager.Instance;
        bool visible = objective != null &&
            (GameSession.IsMatchActive || objective.HasEnded) &&
            !GameSession.IsInPrepPhase;

        if (_root != null)
        {
            _root.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        float progress = Mathf.Clamp01(objective.LocalTeamProgress / TestMapObjectiveManager.VictoryPoints);
        _fill.sizeDelta = new Vector2(294f * progress, -6f);
        _label.text = objective.HasEnded
            ? "OBJECTIVE COMPLETE"
            : $"{TestMapObjectiveManager.MapName.ToUpperInvariant()}  {Mathf.FloorToInt(objective.LocalTeamProgress)}/100";

        if (objective.ActiveUseDrill != null)
        {
            _interactionText.text = $"HOLD T {Mathf.CeilToInt(objective.ActiveUseFraction * 100f)}%";
        }
        else
        {
            _interactionText.text = string.Empty;
        }
    }

    static void CreateBorder(Transform parent)
    {
        CreateBorderBar(parent, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
        CreateBorderBar(parent, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f));
        CreateBorderBar(parent, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 0f));
        CreateBorderBar(parent, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f));
    }

    static void CreateBorderBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;
        var image = go.AddComponent<Image>();
        image.sprite = MenuUiFactory.WhiteSprite;
        image.color = Color.black;
        image.raycastTarget = false;
    }
}
