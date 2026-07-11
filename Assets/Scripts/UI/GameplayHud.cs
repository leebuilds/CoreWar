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
    const float HealthBarHeight = 14f;
    const float HealthBarBaseWidth = 180f;
    const float HealthBarTopMargin = 18f;
    static readonly Color HealthBarRed = new Color(0.86f, 0.12f, 0.1f, 1f);
    static readonly Color HealthBarYellow = new Color(0.96f, 0.88f, 0.12f, 1f);
    static readonly Color HealthBarGreen = new Color(0.14f, 0.74f, 0.24f, 1f);
    const float HealthBarYellowAt = 100f;
    const float HealthBarGreenAt = 200f;
    const float ShieldFlashPeriodFull = 8f;
    const float ShieldFlashPeriodEmpty = 0.1f;
    const float ShieldFastFlashFraction = 0.2f;
    const float ShieldSolidAlpha = 0.95f;
    const float ShieldFaintAlpha = 0.12f;
    static readonly Color ShieldBlue = new Color(0.28f, 0.62f, 1f, 1f);
    static readonly Color CyborgBoostPink = new Color(0.96f, 0.38f, 0.72f, 1f);

    static GameplayHud _instance;

    RectTransform _root;
    RectTransform _crosshairRoot;
    Image _crosshairLeft;
    Image _crosshairRight;
    Image _crosshairTop;
    Image _crosshairBottom;
    Image _crosshairCircle;
    Image _redDot;
    GameObject _scopeLabelRoot;
    Text _scopeLabelText;

    RectTransform _hotbarRoot;
    GameObject _ammoPanelRoot;
    Text _ammoText;
    RectTransform _healthBarRoot;
    Image _healthBarFill;
    Image _healthBarShieldFill;
    float _shieldFlashPhase;
    bool _shieldWasActive;
    float _cyborgBoostFlashPhase;
    bool _cyborgBoostWasActive;
    HotbarSlotView _abilitySlot;
    HotbarSlotView _grenadeSlot;
    readonly List<HotbarSlotView> _equippableSlots = new List<HotbarSlotView>();

    GameObject _buildSelectorRoot;
    RawImage _buildRadialImage;
    readonly List<Text> _buildLabels = new List<Text>();

    GameObject _grenadeSelectorRoot;
    RawImage _grenadeRadialImage;
    readonly List<Text> _grenadeLabels = new List<Text>();

    static Texture2D _crosshairRingTexture;
    static Sprite _crosshairRingSprite;
    static float _crosshairRingThickness;

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
        BuildHealthBar();
        BuildHotbar();
        BuildBuildSelector();
        BuildGrenadeSelector();
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
        RefreshHealthBar(player);

        bool showCrosshair = fullHud && !player.IsHudGameplayBlocked && !player.IsRadialSelectorOpen;
        RefreshCrosshair(player, showCrosshair);
        RefreshBuildSelector(player, fullHud);
        RefreshGrenadeSelector(player, fullHud);
    }

    void BuildCrosshair()
    {
        _crosshairRoot = CreateAnchoredChild(_root, "Crosshair", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        _crosshairLeft = CreateCrosshairBar("Left");
        _crosshairRight = CreateCrosshairBar("Right");
        _crosshairTop = CreateCrosshairBar("Top");
        _crosshairBottom = CreateCrosshairBar("Bottom");

        _crosshairCircle = CreateCrosshairBar("Circle");
        _crosshairCircle.type = Image.Type.Simple;
        _crosshairCircle.preserveAspect = true;
        _crosshairCircle.gameObject.SetActive(false);

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

    void BuildHealthBar()
    {
        _healthBarRoot = CreateAnchoredChild(
            _root,
            "Health Bar",
            new Vector2(0.5f, 1f),
            new Vector2(0f, -HealthBarTopMargin),
            new Vector2(HealthBarBaseWidth, HealthBarHeight));
        _healthBarRoot.pivot = new Vector2(0.5f, 1f);

        var backgroundGo = new GameObject("Background");
        backgroundGo.transform.SetParent(_healthBarRoot, false);
        var backgroundRect = backgroundGo.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(backgroundRect);
        var backgroundImage = backgroundGo.AddComponent<Image>();
        backgroundImage.sprite = MenuUiFactory.WhiteSprite;
        backgroundImage.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
        backgroundImage.raycastTarget = false;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(_healthBarRoot, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        _healthBarFill = fillGo.AddComponent<Image>();
        _healthBarFill.sprite = MenuUiFactory.WhiteSprite;
        _healthBarFill.raycastTarget = false;

        var shieldGo = new GameObject("Shield Fill");
        shieldGo.transform.SetParent(_healthBarRoot, false);
        var shieldRect = shieldGo.AddComponent<RectTransform>();
        shieldRect.anchorMin = new Vector2(0f, 0f);
        shieldRect.anchorMax = new Vector2(1f, 1f);
        shieldRect.offsetMin = new Vector2(2f, 2f);
        shieldRect.offsetMax = new Vector2(-2f, -2f);
        shieldRect.pivot = new Vector2(0f, 0.5f);
        _healthBarShieldFill = shieldGo.AddComponent<Image>();
        _healthBarShieldFill.sprite = MenuUiFactory.WhiteSprite;
        _healthBarShieldFill.raycastTarget = false;
        _healthBarShieldFill.gameObject.SetActive(false);

        AddBorderImages(_healthBarRoot);
    }

    void RefreshHealthBar(ThirdPersonController player)
    {
        if (_healthBarRoot == null || _healthBarFill == null)
        {
            return;
        }

        var health = player.GetComponent<PlayerHealth>();
        if (health == null)
        {
            _healthBarRoot.gameObject.SetActive(false);
            return;
        }

        _healthBarRoot.gameObject.SetActive(true);

        float maxHealth = Mathf.Max(1f, health.MaxHealth);
        float width = HealthBarBaseWidth * (maxHealth / PlayerHealth.BaselineMaxHealth);
        _healthBarRoot.sizeDelta = new Vector2(width, HealthBarHeight);

        float fraction = health.HealthFraction;
        var fillRect = _healthBarFill.rectTransform;
        fillRect.anchorMax = new Vector2(fraction, 1f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        _healthBarFill.color = HealthBarColorForMaxHealth(maxHealth);

        if (health.HasShield)
        {
            if (!_shieldWasActive)
            {
                _shieldFlashPhase = 0f;
            }

            _shieldWasActive = true;
            _cyborgBoostWasActive = false;
            _cyborgBoostFlashPhase = 0f;

            float shieldMax = Mathf.Max(0.01f, player.HeavyShieldMaxForHud);
            float shieldFraction = Mathf.Clamp01(health.ShieldHealth / shieldMax);
            var shieldRect = _healthBarShieldFill.rectTransform;
            shieldRect.anchorMax = Vector2.one;
            shieldRect.offsetMax = new Vector2(-2f, -2f);

            float flashPeriod = shieldFraction <= ShieldFastFlashFraction
                ? ShieldFlashPeriodEmpty
                : Mathf.Lerp(
                    ShieldFlashPeriodEmpty,
                    ShieldFlashPeriodFull,
                    (shieldFraction - ShieldFastFlashFraction) / (1f - ShieldFastFlashFraction));
            _shieldFlashPhase += (Time.deltaTime * Mathf.PI * 2f) / Mathf.Max(0.01f, flashPeriod);
            float pulse = (Mathf.Sin(_shieldFlashPhase) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(ShieldFaintAlpha, ShieldSolidAlpha, pulse);
            _healthBarShieldFill.color = new Color(ShieldBlue.r, ShieldBlue.g, ShieldBlue.b, alpha);
            _healthBarShieldFill.gameObject.SetActive(true);
        }
        else if (health.HasMaxHealthBoost)
        {
            if (!_cyborgBoostWasActive)
            {
                _cyborgBoostFlashPhase = 0f;
            }

            _cyborgBoostWasActive = true;
            _shieldWasActive = false;
            _shieldFlashPhase = 0f;

            var boostRect = _healthBarShieldFill.rectTransform;
            boostRect.anchorMax = Vector2.one;
            boostRect.offsetMax = new Vector2(-2f, -2f);

            _cyborgBoostFlashPhase += (Time.deltaTime * Mathf.PI * 2f) / ShieldFlashPeriodFull;
            float pulse = (Mathf.Sin(_cyborgBoostFlashPhase) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(ShieldFaintAlpha, ShieldSolidAlpha, pulse);
            _healthBarShieldFill.color = new Color(CyborgBoostPink.r, CyborgBoostPink.g, CyborgBoostPink.b, alpha);
            _healthBarShieldFill.gameObject.SetActive(true);
        }
        else
        {
            _shieldWasActive = false;
            _shieldFlashPhase = 0f;
            _cyborgBoostWasActive = false;
            _cyborgBoostFlashPhase = 0f;
            _healthBarShieldFill.gameObject.SetActive(false);
        }
    }

    static Color HealthBarColorForMaxHealth(float maxHealth)
    {
        if (maxHealth <= HealthBarYellowAt)
        {
            float t = Mathf.Clamp01(maxHealth / HealthBarYellowAt);
            return Color.Lerp(HealthBarRed, HealthBarYellow, t);
        }

        float greenBlend = Mathf.Clamp01((maxHealth - HealthBarYellowAt) / (HealthBarGreenAt - HealthBarYellowAt));
        return Color.Lerp(HealthBarYellow, HealthBarGreen, greenBlend);
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
            _equippableSlots.Add(CreateHotbarSlot(_hotbarRoot, $"Slot {i + 1}", new Vector2(EquippableSlotAnchoredX(i), 0f)));
        }

        _grenadeSlot = CreateHotbarSlot(_hotbarRoot, "Grenade", new Vector2(GrenadeSlotAnchoredX(), 0f));
    }

    static float WeaponGroupStartX()
    {
        return SlotSize + GroupGap;
    }

    static float GrenadeSlotAnchoredX()
    {
        return WeaponGroupStartX() + (2f * (SlotSize + SlotGap)) + GroupGap;
    }

    static float EquippableSlotAnchoredX(int index)
    {
        float start = WeaponGroupStartX();
        if (index <= 1)
        {
            return start + (index * (SlotSize + SlotGap));
        }

        return GrenadeSlotAnchoredX() + GroupGap + SlotSize + GroupGap + ((index - 2) * (SlotSize + SlotGap));
    }

    static float HotbarWidthForEquippableCount(int equippableCount)
    {
        float width = WeaponGroupStartX();
        width += Mathf.Min(2, equippableCount) * SlotSize;
        width += Mathf.Max(0, Mathf.Min(2, equippableCount) - 1) * SlotGap;
        if (equippableCount > 0)
        {
            width += GroupGap + SlotSize;
        }

        if (equippableCount > 2)
        {
            width += GroupGap + ((equippableCount - 2) * SlotSize);
            width += (equippableCount - 3) * SlotGap;
            width += GroupGap;
        }

        return width;
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

    void BuildGrenadeSelector()
    {
        _grenadeSelectorRoot = new GameObject("Grenade Selector");
        _grenadeSelectorRoot.transform.SetParent(_root, false);
        var rect = _grenadeSelectorRoot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(168f, 168f);

        var radialGo = new GameObject("Radial");
        radialGo.transform.SetParent(_grenadeSelectorRoot.transform, false);
        MenuUiFactory.StretchFull(radialGo.AddComponent<RectTransform>());
        _grenadeRadialImage = radialGo.AddComponent<RawImage>();
        _grenadeRadialImage.raycastTarget = false;

        for (int i = 0; i < 4; i++)
        {
            var label = MenuUiFactory.CreateAnchoredText(
                _grenadeSelectorRoot.transform,
                $"Grenade Label {i}",
                string.Empty,
                13,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.black);
            _grenadeLabels.Add(label);
        }

        _grenadeSelectorRoot.SetActive(false);
    }

    void RefreshHotbar(ThirdPersonController player)
    {
        var kit = player.ActiveKit;
        int equippableCount = player.EquippableHotbarCount;
        float reloadOverlayFill = player.HotbarReloadOverlayFill;
        float switchLockOverlayFill = player.HotbarSwitchLockOverlayFill;
        float abilityOverlayFill = player.HotbarAbilityOverlayFill;
        float hotbarWidth = HotbarWidthForEquippableCount(equippableCount);

        var hotbarRect = _hotbarRoot;
        hotbarRect.sizeDelta = new Vector2(hotbarWidth, SlotSize);

        bool showAmmo = player.ShowsAmmoHud;
        bool showOverheat = player.UsesOverheatHud;
        _ammoPanelRoot.SetActive(showAmmo || showOverheat);
        if (showOverheat)
        {
            var ammoRect = _ammoPanelRoot.GetComponent<RectTransform>();
            ammoRect.sizeDelta = new Vector2(hotbarWidth, AmmoBarHeight);
            if (player.IsLaserOverheated)
            {
                _ammoText.text = "OVERHEAT";
            }
            else
            {
                int heatPercent = Mathf.RoundToInt(player.LaserHeatFraction * 100f);
                _ammoText.text = $"HEAT {heatPercent}%";
            }
        }
        else if (showAmmo)
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
            Mathf.Max(abilityOverlayFill, Mathf.Max(reloadOverlayFill, switchLockOverlayFill)),
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
            bool selected = !player.IsGrenadeHotbarSelected && i == player.SelectedHotbarIndex;
            float weaponOverlayFill = Mathf.Max(
                Mathf.Max(reloadOverlayFill, switchLockOverlayFill),
                player.HotbarWeaponOverlayFill(tool));
            ApplyHotbarSlot(
                _equippableSlots[i],
                CardKitDefinition.HotbarKeyLabel(i),
                selected
                    ? new Color(0.16f, 0.68f, 0.24f, 0.9f)
                    : new Color(0.96f, 0.96f, 0.96f, 0.72f),
                weaponOverlayFill,
                0.62f,
                true);
            _equippableSlots[i].Icon.texture =
                tool == CardHotbarTool.C4Charge && player.IsC4RemoteSelectedForHud
                    ? HotbarIconDrawer.GetC4RemoteIconTexture()
                    : HotbarIconDrawer.GetToolIconTexture(tool);
            _equippableSlots[i].Icon.gameObject.SetActive(true);
            _equippableSlots[i].ScopeLabel.gameObject.SetActive(false);
        }

        ApplyHotbarSlot(
            _grenadeSlot,
            "Q",
            player.IsGrenadeHotbarSelected
                ? new Color(0.16f, 0.68f, 0.24f, 0.9f)
                : player.HasAnyGrenadesRemaining
                    ? new Color(0.96f, 0.96f, 0.96f, 0.72f)
                    : new Color(0.72f, 0.72f, 0.72f, 0.45f),
            Mathf.Max(
                Mathf.Max(reloadOverlayFill, switchLockOverlayFill),
                player.HotbarWeaponOverlayFill(CardHotbarTool.Grenade)),
            0.62f,
            player.HasAnyGrenadesRemaining);
        _grenadeSlot.Icon.texture = HotbarIconDrawer.GetGrenadeIconTexture(
            player.SelectedGrenadeType,
            !player.HasAnyGrenadesRemaining);
        _grenadeSlot.Icon.gameObject.SetActive(true);
        int selectedGrenadeCount = player.SelectedGrenadeType == GrenadeType.Flashbang
            ? player.FlashbangGrenadesRemaining
            : player.FragGrenadesRemaining;
        bool showGrenadeCount = player.HasAnyGrenadesRemaining && selectedGrenadeCount > 0;
        _grenadeSlot.ScopeLabel.gameObject.SetActive(showGrenadeCount);
        if (showGrenadeCount)
        {
            _grenadeSlot.ScopeLabel.text = selectedGrenadeCount.ToString();
            _grenadeSlot.ScopeLabel.color = new Color(0.92f, 0.12f, 0.1f, 0.98f);
        }
    }

    static void RefreshAbilityIcon(ThirdPersonController player, HotbarSlotView slot, bool dimmed)
    {
        string cardId = player.ActiveCardIdForHud;
        switch (cardId)
        {
            case "sniper_2":
                slot.ScopeLabel.gameObject.SetActive(false);
                slot.Icon.gameObject.SetActive(true);
                slot.Icon.texture = HotbarIconDrawer.GetHunterMarkAbilityIconTexture(dimmed);
                break;
            case "sniper_1":
                int nextScopeIndex = player.SniperScopeIndex == 1 ? 2 : 1;
                slot.Icon.gameObject.SetActive(false);
                slot.ScopeLabel.gameObject.SetActive(true);
                slot.ScopeLabel.text = nextScopeIndex == 1 ? "4X" : "10X";
                slot.ScopeLabel.color = dimmed
                    ? new Color(0.92f, 0.12f, 0.1f, 0.55f)
                    : new Color(0.92f, 0.12f, 0.1f, 1f);
                break;
            case "sniper_3":
                slot.ScopeLabel.gameObject.SetActive(false);
                slot.Icon.gameObject.SetActive(true);
                slot.Icon.texture = HotbarIconDrawer.GetAntiMaterialBraceAbilityIconTexture(dimmed);
                break;
            case "infantry_1":
                slot.ScopeLabel.gameObject.SetActive(false);
                slot.Icon.gameObject.SetActive(true);
                slot.Icon.texture = HotbarIconDrawer.GetInfantryAbilityIconTexture(dimmed);
                break;
            case "infantry_2":
                slot.ScopeLabel.gameObject.SetActive(false);
                slot.Icon.gameObject.SetActive(true);
                slot.Icon.texture = HotbarIconDrawer.GetHoldBreathAbilityIconTexture(dimmed);
                break;
            case "infantry_3":
                slot.ScopeLabel.gameObject.SetActive(false);
                slot.Icon.gameObject.SetActive(true);
                slot.Icon.texture = HotbarIconDrawer.GetDashAbilityIconTexture(dimmed);
                break;
            case "heavy_1":
                slot.ScopeLabel.gameObject.SetActive(false);
                slot.Icon.gameObject.SetActive(true);
                slot.Icon.texture = HotbarIconDrawer.GetShieldAbilityIconTexture(dimmed);
                break;
            case "heavy_2":
                slot.ScopeLabel.gameObject.SetActive(false);
                slot.Icon.gameObject.SetActive(true);
                slot.Icon.texture = HotbarIconDrawer.GetCyborgRegenAbilityIconTexture(dimmed);
                break;
            case "demolition_1":
                slot.ScopeLabel.gameObject.SetActive(false);
                slot.Icon.gameObject.SetActive(true);
                slot.Icon.texture = HotbarIconDrawer.GetExplosiveVestAbilityIconTexture(dimmed);
                break;
            case "gunner_1":
                slot.ScopeLabel.gameObject.SetActive(false);
                slot.Icon.gameObject.SetActive(true);
                slot.Icon.texture = HotbarIconDrawer.GetGunnerSuppressionAbilityIconTexture(dimmed);
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
            out float scopeRadiusFraction,
            out bool showCircle,
            out float circleRadius,
            out float circleThickness);

        _redDot.gameObject.SetActive(showRedDot);
        _scopeLabelRoot.SetActive(showScopeLabel);

        bool showBars = showStandard && !showRedDot && !showCircle;
        _crosshairLeft.gameObject.SetActive(showBars);
        _crosshairRight.gameObject.SetActive(showBars);
        _crosshairTop.gameObject.SetActive(showBars);
        _crosshairBottom.gameObject.SetActive(showBars);
        _crosshairCircle.gameObject.SetActive(showCircle && !showRedDot);

        if (showCircle && !showRedDot)
        {
            float diameter = Mathf.Max(8f, circleRadius * 2f);
            _crosshairCircle.sprite = GetCrosshairRingSprite(circleThickness);
            _crosshairCircle.color = color;
            var circleRect = _crosshairCircle.rectTransform;
            circleRect.anchorMin = new Vector2(0.5f, 0.5f);
            circleRect.anchorMax = new Vector2(0.5f, 0.5f);
            circleRect.pivot = new Vector2(0.5f, 0.5f);
            circleRect.anchoredPosition = Vector2.zero;
            circleRect.sizeDelta = new Vector2(diameter, diameter);
        }

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
            _scopeLabelText.text = scopeIndex switch
            {
                1 => "4X",
                2 => "10X",
                3 => "1.8X",
                4 => "12X",
                _ => "IRON"
            };
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

    static Sprite GetCrosshairRingSprite(float thicknessPixels)
    {
        float thickness = Mathf.Clamp(thicknessPixels, 1f, 8f);
        if (_crosshairRingSprite != null && Mathf.Approximately(_crosshairRingThickness, thickness))
        {
            return _crosshairRingSprite;
        }

        const int size = 128;
        if (_crosshairRingTexture == null)
        {
            _crosshairRingTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        _crosshairRingThickness = thickness;
        float center = (size - 1) * 0.5f;
        float outerRadius = center - 1f;
        float innerRadius = Mathf.Max(0f, outerRadius - thickness);
        var clear = new Color(1f, 1f, 1f, 0f);
        var white = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                bool inRing = distance <= outerRadius && distance >= innerRadius;
                _crosshairRingTexture.SetPixel(x, y, inRing ? white : clear);
            }
        }

        _crosshairRingTexture.Apply();
        if (_crosshairRingSprite != null)
        {
            Destroy(_crosshairRingSprite);
        }

        _crosshairRingSprite = Sprite.Create(
            _crosshairRingTexture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
        return _crosshairRingSprite;
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

    void RefreshGrenadeSelector(ThirdPersonController player, bool fullHud)
    {
        if (!fullHud || !player.IsGrenadeSelectorOpen)
        {
            _grenadeSelectorRoot.SetActive(false);
            return;
        }

        _grenadeSelectorRoot.SetActive(true);
        float radius = player.GrenadeSelectorRadius;
        var rect = _grenadeSelectorRoot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(radius * 2f, radius * 2f);
        _grenadeRadialImage.texture = player.GetGrenadeSelectorTexture();

        int optionCount = player.GrenadeOptionCount;
        for (int i = 0; i < _grenadeLabels.Count; i++)
        {
            bool visible = i < optionCount;
            string labelText = visible ? player.GetGrenadeDisplayName(i) : string.Empty;
            visible = visible && !string.IsNullOrEmpty(labelText);
            _grenadeLabels[i].gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            float angle = i * (360f / optionCount) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * 0.72f);
            var labelRect = _grenadeLabels[i].GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(offset.x, -offset.y);
            labelRect.sizeDelta = new Vector2(92f, 24f);
            _grenadeLabels[i].text = labelText.ToUpperInvariant();
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
