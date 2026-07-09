using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space hunter mark icons positioned with Camera.WorldToScreenPoint.
/// </summary>
public class HunterMarkOverlay : MonoBehaviour
{
    const float BaseAspect = 56f / 154f;
    const float HeadCenterYOffset = 1.52f;
    const float HeadHalfWidth = 0.18f;
    const float LineBottomYOffset = 0.05f;
    const float MinScreenHeightPixels = 12f;

    static HunterMarkOverlay _instance;

    readonly List<MarkEntry> _activeMarks = new List<MarkEntry>();
    readonly Stack<MarkEntry> _pool = new Stack<MarkEntry>();

    RectTransform _root;
    Camera _camera;

    struct MarkEntry
    {
        public Transform target;
        public ShootingRangeDummy dummy;
        public RectTransform rect;
        public Image image;
    }

    public static HunterMarkOverlay EnsureExists()
    {
        if (_instance != null)
        {
            return _instance;
        }

        GameUICanvas.EnsureExists();
        var layer = GameUICanvas.CreateLayer("Hunter Marks");
        layer.SetAsFirstSibling();
        var host = GameUICanvas.CreateScreenHost(layer, "Hunter Mark Overlay");
        _instance = host.gameObject.AddComponent<HunterMarkOverlay>();
        _instance.Build(host);
        return _instance;
    }

    void Build(RectTransform host)
    {
        _root = host;
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public static void ShowMarks(Camera camera, IReadOnlyList<Transform> targets, IReadOnlyList<Vector3> worldPositions)
    {
        if (camera == null || targets == null || targets.Count == 0)
        {
            return;
        }

        var overlay = EnsureExists();
        overlay.ResetMarks();
        overlay._camera = camera;

        for (int i = 0; i < targets.Count; i++)
        {
            Transform target = targets[i];
            if (target == null)
            {
                continue;
            }

            overlay.AddMark(target);
        }
    }

    public static void ClearMarks()
    {
        if (_instance == null)
        {
            return;
        }

        _instance.ResetMarks();
    }

    void ResetMarks()
    {
        for (int i = 0; i < _activeMarks.Count; i++)
        {
            ReturnToPool(_activeMarks[i]);
        }

        _activeMarks.Clear();
        _camera = null;
    }

    void AddMark(Transform target)
    {
        MarkEntry entry = _pool.Count > 0 ? _pool.Pop() : CreateIcon();
        entry.target = target;
        entry.dummy = target.GetComponent<ShootingRangeDummy>();
        entry.rect.gameObject.SetActive(true);
        entry.image.sprite = HunterMarkOutlineDrawer.GetTargetMarkSprite();
        entry.image.color = Color.white;
        _activeMarks.Add(entry);
        UpdateEntry(entry);
    }

    MarkEntry CreateIcon()
    {
        var go = new GameObject("Hunter Mark Icon");
        go.transform.SetParent(_root, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(56f, 154f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, HunterMarkOutlineDrawer.PivotYNormalized);

        var image = go.AddComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;

        return new MarkEntry
        {
            rect = rect,
            image = image
        };
    }

    void ReturnToPool(MarkEntry entry)
    {
        entry.target = null;
        entry.dummy = null;
        if (entry.rect != null)
        {
            entry.rect.gameObject.SetActive(false);
        }

        _pool.Push(entry);
    }

    void LateUpdate()
    {
        if (_camera == null || _activeMarks.Count == 0)
        {
            return;
        }

        for (int i = _activeMarks.Count - 1; i >= 0; i--)
        {
            MarkEntry entry = _activeMarks[i];
            if (entry.target == null || IsTargetEliminated(entry))
            {
                ReturnToPool(entry);
                _activeMarks.RemoveAt(i);
                continue;
            }

            UpdateEntry(entry);
        }
    }

    static bool IsTargetEliminated(MarkEntry entry)
    {
        if (entry.dummy != null)
        {
            return entry.dummy.IsDown;
        }

        return false;
    }

    void UpdateEntry(MarkEntry entry)
    {
        if (entry.target == null)
        {
            entry.rect.gameObject.SetActive(false);
            return;
        }

        float headCenterY = entry.dummy != null ? 1.52f : HeadCenterYOffset;
        float headHalfWidth = entry.dummy != null ? entry.dummy.HeadMarkHalfWidth : HeadHalfWidth;
        float lineBottomY = entry.dummy != null ? entry.dummy.MarkLineBottomOffset : LineBottomYOffset;

        Vector3 headCenter = entry.target.position + (Vector3.up * headCenterY);
        Vector3 lineBottom = entry.target.position + (Vector3.up * lineBottomY);
        Vector3 headLeft = headCenter - (entry.target.right * headHalfWidth);
        Vector3 headRight = headCenter + (entry.target.right * headHalfWidth);

        Vector3 headCenterScreen = _camera.WorldToScreenPoint(headCenter);
        Vector3 lineBottomScreen = _camera.WorldToScreenPoint(lineBottom);
        Vector3 headLeftScreen = _camera.WorldToScreenPoint(headLeft);
        Vector3 headRightScreen = _camera.WorldToScreenPoint(headRight);

        if (headCenterScreen.z <= 0f)
        {
            entry.rect.gameObject.SetActive(false);
            return;
        }

        entry.rect.gameObject.SetActive(true);

        float belowPivotNorm = HunterMarkOutlineDrawer.PivotYNormalized - HunterMarkOutlineDrawer.SpriteBottomYNormalized;
        float abovePivotNorm = HunterMarkOutlineDrawer.SpriteTopArtYNormalized - HunterMarkOutlineDrawer.PivotYNormalized;
        float belowPivotScreen = Mathf.Abs(headCenterScreen.y - lineBottomScreen.y);
        float abovePivotScreen = belowPivotScreen * (abovePivotNorm / Mathf.Max(0.0001f, belowPivotNorm));
        float screenHeight = Mathf.Max(belowPivotScreen + abovePivotScreen, MinScreenHeightPixels);
        float screenWidth = Mathf.Max(Mathf.Abs(headRightScreen.x - headLeftScreen.x) * 1.5f, screenHeight * BaseAspect);
        float canvasScale = GetCanvasScaleFactor();

        entry.rect.sizeDelta = new Vector2(screenWidth / canvasScale, screenHeight / canvasScale);
        entry.rect.localScale = Vector3.one;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _root,
            headCenterScreen,
            null,
            out Vector2 localPoint))
        {
            entry.rect.anchoredPosition = localPoint;
        }
    }

    float GetCanvasScaleFactor()
    {
        var canvas = _root != null ? _root.GetComponentInParent<Canvas>() : null;
        return canvas != null ? Mathf.Max(0.0001f, canvas.scaleFactor) : 1f;
    }
}
