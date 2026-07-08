using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single in-game UI canvas (scene object or runtime fallback). All match HUD and
/// overlays parent here and share one CanvasScaler for consistent screen scaling.
/// </summary>
public class GameUICanvas : MonoBehaviour
{
    public const string PreferredObjectName = "Game UI Canvas";

    static GameUICanvas _instance;

    RectTransform _root;

    public static GameUICanvas Instance => _instance;
    public static RectTransform Root => _instance != null ? _instance._root : null;
    public static bool IsReady => _instance != null && _instance._root != null;

    public static GameUICanvas EnsureExists()
    {
        if (_instance != null)
        {
            MenuUiFactory.EnsureEventSystem();
            return _instance;
        }

        var existing = FindAnyObjectByType<GameUICanvas>();
        if (existing != null)
        {
            existing.Configure();
            return existing;
        }

        GameObject host = GameObject.Find(PreferredObjectName);
        if (host == null)
        {
            host = GameObject.Find("Canvas");
        }

        if (host == null)
        {
            host = new GameObject(PreferredObjectName);
        }

        var canvas = host.GetComponent<GameUICanvas>() ?? host.AddComponent<GameUICanvas>();
        canvas.Configure();
        return canvas;
    }

    public static RectTransform CreateLayer(string name)
    {
        EnsureExists();
        var layerGo = new GameObject(name);
        layerGo.transform.SetParent(_instance._root, false);
        var rect = layerGo.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(rect);
        return rect;
    }

    /// <summary>
    /// Full-screen layer with its own canvas sort order so buttons receive clicks above the HUD.
    /// </summary>
    public static RectTransform CreateInteractionLayer(string name, int sortingOrder = 100)
    {
        var layer = CreateLayer(name);
        var canvas = layer.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        layer.gameObject.AddComponent<GraphicRaycaster>();
        return layer;
    }

    public static void BringLayerToFront(RectTransform layer)
    {
        if (layer != null)
        {
            layer.SetAsLastSibling();
        }
    }

    public static RectTransform CreateScreenHost(Transform parent, string name)
    {
        var hostGo = new GameObject(name);
        hostGo.transform.SetParent(parent, false);
        var rect = hostGo.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(rect);
        return rect;
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        Configure();
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    void Configure()
    {
        if (_instance == null)
        {
            _instance = this;
        }

        var canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        EnsureRoot();
        MenuUiFactory.EnsureEventSystem();
    }

    void EnsureRoot()
    {
        Transform existing = transform.Find("Root");
        if (existing != null)
        {
            _root = existing as RectTransform;
            if (_root != null)
            {
                MenuUiFactory.StretchFull(_root);
                return;
            }
        }

        var rootGo = new GameObject("Root");
        rootGo.transform.SetParent(transform, false);
        _root = rootGo.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(_root);
    }
}
