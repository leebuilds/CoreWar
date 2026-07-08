using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-match gameplay HUD (crosshair, hotbar, build selector) on the shared game canvas.
/// </summary>
public class GameplayHud : MonoBehaviour
{
    const float SlotSize = 36f;
    const float SlotGap = 6f;
    const float GroupGap = 14f;
    const float Margin = 16f;
    const float HotbarScale = 2f;
    const float CrosshairScale = 2.5f;
    const float AmmoGapAboveHotbar = 4f;
    const float AmmoBarHeight = 18f;

    static GameplayHud _instance;

    RectTransform _root;
    RectTransform _crosshairRoot;
    Image _crosshairLeft;
    Image _crosshairRight;
    Image _crosshairTop;
    Image _crosshairBottom;
    Image _redDot;
    GameObject _scopeLabelRoot;
    Text _scopeLabelText;

    RectTransform _hotbarRoot;
    GameObject _ammoPanelRoot;
    Text _ammoText;
    HotbarSlotView _abilitySlot;
    readonly List<HotbarSlotView> _equippableSlots = new List<HotbarSlotView>();

    GameObject _buildSelectorRoot;
    RawImage _buildRadialImage;
    readonly List<Text> _buildLabels = new List<Text>();

    sealed class HotbarSlotView
    {
        public GameObject Root;
        public Image Background;
        public Image Overlay;
        public Text KeyLabel;
        public RawImage Icon;
        public Text ScopeLabel;
    }

    public static GameplayHud Create()
    {
        if (_instance != null)
        {
            return _instance;
        }

        GameUICanvas.EnsureExists();
        var layer = GameUICanvas.CreateLayer("Gameplay HUD");
        var host = layer.gameObject.AddComponent<GameplayHud>();
        host.Build(layer);
        return host;
    }

    void Build(RectTransform layer)
    {
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(layer, false);
        _root = contentGo.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(_root);
        BuildCrosshair();
        BuildHotbar();
        BuildBuildSelector();
        _crosshairRoot.localScale = Vector3.one * CrosshairScale;
        _root.gameObject.SetActive(false);
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

    void LateUpdate()
    {
        Refresh();
    }

    void Refresh()
    {
        if (!GameSession.IsMatchActive || !SceneFlow.IsGameActive)
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }

            return;
        }

        var player = ThirdPersonController.Local;
        if (player == null)
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }

            return;
        }

        if (player.IsHudOverlayBlocking)
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }

            return;
        }

        bool prepOnlyHotbar = GameSession.IsInPrepPhase && !GameSession.IsPrepReady;
        bool fullHud = !GameSession.IsInPrepPhase;

        if (prepOnlyHotbar)
        {
            _root.gameObject.SetActive(false);
            return;
        }

        _root.gameObject.SetActive(true);
        RefreshHotbar(player);

        bool showCrosshair = fullHud && !player.IsHudGameplayBlocked;
        RefreshCrosshair(player, showCrosshair);
        RefreshBuildSelector(player, fullHud);
    }

    void BuildCrosshair()
    {
        _crosshairRoot = CreateAnchoredChild(_root, "Crosshair", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        _crosshairLeft = CreateCrosshairBar("Left");
        _crosshairRight = CreateCrosshairBar("Right");
        _crosshairTop = CreateCrosshairBar("Top");
        _crosshairBottom = CreateCrosshairBar("Bottom");

        _redDot = CreateCrosshairBar("Red Dot");
        _redDot.color = new Color(0.92f, 0.12f, 0.1f, 0.95f);
        _redDot.rectTransform.sizeDelta = new Vector2(6f, 6f);

        _scopeLabelRoot = new GameObject("Scope Label");
        _scopeLabelRoot.transform.SetParent(_crosshairRoot, false);
        var panelRect = _scopeLabelRoot.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(72f, 24f);
        var panelImage = _scopeLabelRoot.AddComponent<Image>();
        panelImage.sprite = MenuUiFactory.WhiteSprite;
        panelImage.color = new Color(0.04f, 0.04f, 0.04f, 0.72f);
        panelImage.raycastTarget = false;

        _scopeLabelText = MenuUiFactory.CreateAnchoredText(
            _scopeLabelRoot.transform,
            "Label",
            "4X",
            13,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            new Color(0.92f, 0.12f, 0.1f, 0.98f));
        MenuUiFactory.StretchFull(_scopeLabelText.GetComponent<RectTransform>());
        _scopeLabelRoot.SetActive(false);
    }

    Image CreateCrosshairBar(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_crosshairRoot, false);
        var image = go.AddComponent<Image>();
        image.sprite = MenuUiFactory.WhiteSprite;
        image.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        image.raycastTarget = false;
        return image;
    }

    void BuildHotbar()
    {
        _hotbarRoot = CreateAnchoredChild(
            _root,
            "Hotbar",
            new Vector2(0f, 0f),
            new Vector2(Margin, Margin),
            new Vector2(400f, SlotSize));
        _hotbarRoot.localScale = Vector3.one * HotbarScale;

        _ammoPanelRoot = new GameObject("Ammo Panel");
        _ammoPanelRoot.transform.SetParent(_hotbarRoot, false);
        var ammoRect = _ammoPanelRoot.AddComponent<RectTransform>();
        ammoRect.anchorMin = new Vector2(0f, 1f);
        ammoRect.anchorMax = new Vector2(0f, 1f);
        ammoRect.pivot = new Vector2(0f, 0f);
        ammoRect.anchoredPosition = new Vector2(0f, AmmoGapAboveHotbar);
        ammoRect.sizeDelta = new Vector2(220f, AmmoBarHeight);

        var ammoBg = _ammoPanelRoot.AddComponent<Image>();
        ammoBg.sprite = MenuUiFactory.WhiteSprite;
        ammoBg.color = Color.white;
        ammoBg.raycastTarget = false;
        AddBorderImages(_ammoPanelRoot.transform);

        _ammoText = MenuUiFactory.CreateAnchoredText(
            _ammoPanelRoot.transform,
            "Ammo",
            "0 / 0",
            10,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            Color.black);
        MenuUiFactory.StretchFull(_ammoText.GetComponent<RectTransform>());
        _ammoPanelRoot.SetActive(false);

        _abilitySlot = CreateHotbarSlot(_hotbarRoot, "Ability", new Vector2(0f, 0f));

        for (int i = 0; i < 4; i++)
        {
            float x = SlotSize + GroupGap + (i * (SlotSize + SlotGap)) + (i >= 2 ? GroupGap : 0f);
            _equippableSlots.Add(CreateHotbarSlot(_hotbarRoot, $"Slot {i + 1}", new Vector2(x, 0f)));
        }
    }

    HotbarSlotView CreateHotbarSlot(Transform parent, string name, Vector2 anchoredPosition)
    {
        var view = new HotbarSlotView();
        view.Root = new GameObject(name);
        view.Root.transform.SetParent(parent, false);
        var rect = view.Root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(SlotSize, SlotSize);

        view.Background = view.Root.AddComponent<Image>();
        view.Background.sprite = MenuUiFactory.WhiteSprite;
        view.Background.raycastTarget = false;
        AddBorderImages(view.Root.transform);

        var overlayGo = new GameObject("Overlay");
        overlayGo.transform.SetParent(view.Root.transform, false);
        var overlayRect = overlayGo.AddComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0f, 0f);
        overlayRect.anchorMax = new Vector2(1f, 0f);
        overlayRect.pivot = new Vector2(0.5f, 0f);
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = Vector2.zero;
        view.Overlay = overlayGo.AddComponent<Image>();
        view.Overlay.sprite = MenuUiFactory.WhiteSprite;
        view.Overlay.color = new Color(0.04f, 0.04f, 0.04f, 0.62f);
        view.Overlay.raycastTarget = false;
        view.Overlay.gameObject.SetActive(false);

        view.KeyLabel = MenuUiFactory.CreateAnchoredText(
            view.Root.transform,
            "Key",
            "1",
            8,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            new Color(0.2f, 0.2f, 0.2f, 0.9f));
        var keyRect = view.KeyLabel.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0f, 1f);
        keyRect.anchorMax = new Vector2(0f, 1f);
        keyRect.pivot = new Vector2(0f, 1f);
        keyRect.anchoredPosition = new Vector2(4f, -2f);
        keyRect.sizeDelta = new Vector2(18f, 12f);

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(view.Root.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(iconRect);
        var iconInset = iconRect;
        iconInset.offsetMin = new Vector2(7f, 7f);
        iconInset.offsetMax = new Vector2(-7f, -7f);
        view.Icon = iconGo.AddComponent<RawImage>();
        view.Icon.raycastTarget = false;

        var scopeGo = new GameObject("Scope Label");
        scopeGo.transform.SetParent(view.Root.transform, false);
        MenuUiFactory.StretchFull(scopeGo.AddComponent<RectTransform>());
        view.ScopeLabel = MenuUiFactory.CreateAnchoredText(
            scopeGo.transform,
            "Scope",
            string.Empty,
            10,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            new Color(0.92f, 0.12f, 0.1f, 1f));
        MenuUiFactory.StretchFull(view.ScopeLabel.GetComponent<RectTransform>());
        view.ScopeLabel.gameObject.SetActive(false);

        return view;
    }

    void BuildBuildSelector()
    {
        _buildSelectorRoot = new GameObject("Build Selector");
        _buildSelectorRoot.transform.SetParent(_root, false);
        var rect = _buildSelectorRoot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(168f, 168f);

        var radialGo = new GameObject("Radial");
        radialGo.transform.SetParent(_buildSelectorRoot.transform, false);
        MenuUiFactory.StretchFull(radialGo.AddComponent<RectTransform>());
        _buildRadialImage = radialGo.AddComponent<RawImage>();
        _buildRadialImage.raycastTarget = false;

        for (int i = 0; i < 8; i++)
        {
            var label = MenuUiFactory.CreateAnchoredText(
                _buildSelectorRoot.transform,
                $"Label {i}",
                string.Empty,
                13,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.black);
            _buildLabels.Add(label);
        }

        _buildSelectorRoot.SetActive(false);
    }

    void RefreshHotbar(ThirdPersonController player)
    {
        var kit = player.ActiveKit;
        int equippableCount = player.EquippableHotbarCount;
        float reloadOverlayFill = player.HotbarReloadOverlayFill;
        float abilityOverlayFill = player.HotbarAbilityOverlayFill;
        float hotbarWidth = SlotSize + GroupGap +
            (equippableCount * SlotSize) + ((equippableCount - 1) * SlotGap) +
            (equippableCount > 2 ? GroupGap : 0f);

        var hotbarRect = _hotbarRoot;
        hotbarRect.sizeDelta = new Vector2(hotbarWidth, SlotSize);

        bool showAmmo = player.IsFirearmSelected;
        _ammoPanelRoot.SetActive(showAmmo);
        if (showAmmo)
        {
            var ammoRect = _ammoPanelRoot.GetComponent<RectTransform>();
            ammoRect.sizeDelta = new Vector2(hotbarWidth, AmmoBarHeight);
            var pool = player.CurrentAmmo;
            _ammoText.text = $"{pool.reserve} / {pool.mag}";
        }

        bool abilityReady = player.IsAbilityReadyForHud;
        ApplyHotbarSlot(
            _abilitySlot,
            "E",
            abilityReady
                ? new Color(0.96f, 0.96f, 0.96f, 0.92f)
                : new Color(0.34f, 0.34f, 0.36f, 0.88f),
            Mathf.Max(abilityOverlayFill, reloadOverlayFill),
            abilityReady ? 0.35f : 0.62f,
            abilityReady);

        RefreshAbilityIcon(player, _abilitySlot, !abilityReady);

        for (int i = 0; i < _equippableSlots.Count; i++)
        {
            bool visible = i < equippableCount;
            _equippableSlots[i].Root.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            var tool = kit.GetToolAt(i);
            bool selected = i == player.SelectedHotbarIndex;
            ApplyHotbarSlot(
                _equippableSlots[i],
                CardKitDefinition.HotbarKeyLabel(i),
                selected
                    ? new Color(0.16f, 0.68f, 0.24f, 0.9f)
                    : new Color(0.96f, 0.96f, 0.96f, 0.72f),
                reloadOverlayFill,
                0.62f,
                true);
            _equippableSlots[i].Icon.texture = HotbarIconDrawer.GetToolIconTexture(tool);
            _equippableSlots[i].Icon.gameObject.SetActive(true);
            _equippableSlots[i].ScopeLabel.gameObject.SetActive(false);
        }
    }

    static void RefreshAbilityIcon(ThirdPersonController player, HotbarSlotView slot, bool dimmed)
    {
        string specialty = player.ActiveCardSpecialtyForHud;
        switch (specialty)
        {
            case "sniper":
                int scopeIndex = (player.SniperScopeIndex + 1) % 3;
                if (scopeIndex == 1 || scopeIndex == 2)
                {
                    slot.Icon.gameObject.SetActive(false);
                    slot.ScopeLabel.gameObject.SetActive(true);
                    slot.ScopeLabel.text = scopeIndex == 1 ? "4X" : "10X";
                    slot.ScopeLabel.color = dimmed
                        ? new Color(0.92f, 0.12f, 0.1f, 0.55f)
                        : new Color(0.92f, 0.12f, 0.1f, 1f);
                }
                else
                {
                    slot.ScopeLabel.gameObject.SetActive(false);
                    slot.Icon.gameObject.SetActive(true);
                    slot.Icon.texture = HotbarIconDrawer.GetIronSightIconTexture(dimmed);
                }

                break;
            case "infantry":
                slot.ScopeLabel.gameObject.SetActive(false);
                slot.Icon.gameObject.SetActive(true);
                slot.Icon.texture = HotbarIconDrawer.GetInfantryAbilityIconTexture(dimmed);
                break;
            default:
                slot.Icon.gameObject.SetActive(false);
                slot.ScopeLabel.gameObject.SetActive(false);
                break;
        }
    }

    static void ApplyHotbarSlot(
        HotbarSlotView slot,
        string keyLabel,
        Color backgroundColor,
        float overlayFill,
        float overlayAlpha,
        bool brightKey)
    {
        slot.Background.color = backgroundColor;
        slot.KeyLabel.text = keyLabel;
        slot.KeyLabel.color = brightKey
            ? Color.white
            : new Color(0.82f, 0.82f, 0.82f, 0.85f);

        if (overlayFill > 0.001f)
        {
            slot.Overlay.gameObject.SetActive(true);
            slot.Overlay.color = new Color(0.04f, 0.04f, 0.04f, overlayAlpha);
            var overlayRect = slot.Overlay.rectTransform;
            overlayRect.anchorMin = new Vector2(0f, 0f);
            overlayRect.anchorMax = new Vector2(1f, overlayFill);
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
        }
        else
        {
            slot.Overlay.gameObject.SetActive(false);
        }
    }

    void RefreshCrosshair(ThirdPersonController player, bool showCrosshair)
    {
        _crosshairRoot.gameObject.SetActive(showCrosshair);
        if (!showCrosshair)
        {
            return;
        }

        player.GetCrosshairPresentation(
            out bool showStandard,
            out float gap,
            out float length,
            out float thickness,
            out Color color,
            out bool showRedDot,
            out bool showScopeLabel,
            out int scopeIndex,
            out float scopeRadiusFraction);

        _redDot.gameObject.SetActive(showRedDot);
        _scopeLabelRoot.SetActive(showScopeLabel);

        bool showBars = showStandard && !showRedDot;
        _crosshairLeft.gameObject.SetActive(showBars);
        _crosshairRight.gameObject.SetActive(showBars);
        _crosshairTop.gameObject.SetActive(showBars);
        _crosshairBottom.gameObject.SetActive(showBars);

        if (showBars)
        {
            float halfThickness = thickness * 0.5f;
            SetCrosshairBar(_crosshairLeft, new Vector2(-gap - length, -halfThickness), new Vector2(length, thickness), color);
            SetCrosshairBar(_crosshairRight, new Vector2(gap, -halfThickness), new Vector2(length, thickness), color);
            SetCrosshairBar(_crosshairTop, new Vector2(-halfThickness, gap), new Vector2(thickness, length), color);
            SetCrosshairBar(_crosshairBottom, new Vector2(-halfThickness, -gap - length), new Vector2(thickness, length), color);
        }

        if (showScopeLabel)
        {
            float scopeRadiusPx = 1080f * scopeRadiusFraction;
            var panelRect = _scopeLabelRoot.GetComponent<RectTransform>();
            panelRect.anchoredPosition = new Vector2(0f, scopeRadiusPx + 34f);
            _scopeLabelText.text = scopeIndex == 1 ? "4x" : scopeIndex == 2 ? "10x" : "IRON";
        }
    }

    static void SetCrosshairBar(Image image, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        image.color = color;
    }

    void RefreshBuildSelector(ThirdPersonController player, bool fullHud)
    {
        if (!fullHud || !player.IsBuildSelectorOpen)
        {
            _buildSelectorRoot.SetActive(false);
            return;
        }

        _buildSelectorRoot.SetActive(true);
        float radius = player.BuildSelectorRadius;
        var rect = _buildSelectorRoot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(radius * 2f, radius * 2f);
        _buildRadialImage.texture = player.GetBuildSelectorTexture();

        int optionCount = player.BuildPieceOptionCount;
        for (int i = 0; i < _buildLabels.Count; i++)
        {
            bool visible = i < optionCount;
            _buildLabels[i].gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            float angle = i * (360f / optionCount) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * 0.72f);
            var labelRect = _buildLabels[i].GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(offset.x, -offset.y);
            labelRect.sizeDelta = new Vector2(92f, 24f);
            _buildLabels[i].text = player.GetBuildPieceDisplayName(i).ToUpperInvariant();
        }
    }

    static RectTransform CreateAnchoredChild(
        RectTransform parent,
        string name,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    static void AddBorderImages(Transform parent)
    {
        CreateBorderBar(parent, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 1f));
        CreateBorderBar(parent, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 1f));
        CreateBorderBar(parent, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(1f, 0f));
        CreateBorderBar(parent, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(1f, 0f));
    }

    static void CreateBorderBar(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        var image = go.AddComponent<Image>();
        image.sprite = MenuUiFactory.WhiteSprite;
        image.color = Color.black;
        image.raycastTarget = false;
    }
}
