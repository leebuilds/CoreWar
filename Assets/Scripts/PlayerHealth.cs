using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Player health from the active class card. In multiplayer, the server owns health and replicates it.
/// </summary>
public class PlayerHealth : NetworkBehaviour
{
    public const float BaselineMaxHealth = 100f;
    public const float DefaultRegenDelaySeconds = 4f;
    public const float CyborgRegenDelaySeconds = 2f;
    public const float RegenHealthFractionPerSecond = 0.1f;
    public const float CyborgMaxHealthBoostFraction = 0.15f;

    readonly NetworkVariable<float> _networkCurrentHealth = new NetworkVariable<float>(
        BaselineMaxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<float> _networkCardMaxHealth = new NetworkVariable<float>(
        BaselineMaxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<float> _networkMaxHealth = new NetworkVariable<float>(
        BaselineMaxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<float> _networkShieldHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<bool> _networkMaxHealthBoostActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<bool> _networkIsDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    float _currentHealth;
    float _cardMaxHealth;
    float _maxHealth;
    float _shieldHealth;
    float _secondsSinceHealthDamage;
    float _regenDelaySeconds = DefaultRegenDelaySeconds;
    float _abilityRegenFractionPerSecond;
    bool _maxHealthBoostActive;
    bool _isDead;

    public float CurrentHealth => IsSpawned ? _networkCurrentHealth.Value : _currentHealth;
    public float MaxHealth => IsSpawned ? _networkMaxHealth.Value : _maxHealth;
    public bool IsAlive => IsSpawned ? !_networkIsDead.Value : !_isDead;
    public float BaseMaxHealth => IsSpawned ? _networkCardMaxHealth.Value : _cardMaxHealth;
    public float ShieldHealth => IsSpawned ? _networkShieldHealth.Value : _shieldHealth;
    public bool HasShield => ShieldHealth > 0f;
    public bool HasMaxHealthBoost => IsSpawned ? _networkMaxHealthBoostActive.Value : _maxHealthBoostActive;
    public float ShieldActivatedTime { get; private set; } = -1f;
    public float HealthFraction => MaxHealth > 0f ? Mathf.Clamp01(CurrentHealth / MaxHealth) : 0f;
    public bool UsesModifiedMaxHealth => Mathf.Abs(MaxHealth - BaselineMaxHealth) > 0.01f;
    public bool IsRegenerating =>
        CurrentHealth < MaxHealth &&
        !IsBlindnessBlockingRegen() &&
        (_secondsSinceHealthDamage >= _regenDelaySeconds || _abilityRegenFractionPerSecond > 0f);

    void Start()
    {
        if (IsSpawned && !IsServer)
        {
            PullNetworkState();
            return;
        }

        RefillHealth();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _networkCurrentHealth.OnValueChanged += HandleNetworkFloatChanged;
        _networkCardMaxHealth.OnValueChanged += HandleNetworkFloatChanged;
        _networkMaxHealth.OnValueChanged += HandleNetworkFloatChanged;
        _networkShieldHealth.OnValueChanged += HandleNetworkFloatChanged;
        _networkMaxHealthBoostActive.OnValueChanged += HandleNetworkBoolChanged;
        _networkIsDead.OnValueChanged += HandleNetworkBoolChanged;

        if (IsServer)
        {
            RefillHealth();
        }
        else
        {
            PullNetworkState();
        }
    }

    public override void OnNetworkDespawn()
    {
        _networkCurrentHealth.OnValueChanged -= HandleNetworkFloatChanged;
        _networkCardMaxHealth.OnValueChanged -= HandleNetworkFloatChanged;
        _networkMaxHealth.OnValueChanged -= HandleNetworkFloatChanged;
        _networkShieldHealth.OnValueChanged -= HandleNetworkFloatChanged;
        _networkMaxHealthBoostActive.OnValueChanged -= HandleNetworkBoolChanged;
        _networkIsDead.OnValueChanged -= HandleNetworkBoolChanged;

        base.OnNetworkDespawn();
    }

    void Update()
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        TickHealthRegeneration();
    }

    public void RefillHealth()
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        _isDead = false;
        var card = CardCatalog.Get(GameSession.ActiveCardId);
        _cardMaxHealth = Mathf.Max(1f, card?.preview.health ?? BaselineMaxHealth);
        _maxHealthBoostActive = false;
        _maxHealth = _cardMaxHealth;
        _currentHealth = _maxHealth;
        _shieldHealth = 0f;
        ShieldActivatedTime = -1f;
        _secondsSinceHealthDamage = 0f;
        _abilityRegenFractionPerSecond = 0f;
        ConfigureRegenDelayFromCard(card?.id);
        PushNetworkState();
    }

    public void SetAbilityRegeneration(float fractionPerSecond)
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        _abilityRegenFractionPerSecond = Mathf.Max(0f, fractionPerSecond);
    }

    public void ActivateMaxHealthBoost(float bonusFraction)
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        bonusFraction = Mathf.Max(0f, bonusFraction);
        if (_maxHealthBoostActive || bonusFraction <= 0f)
        {
            return;
        }

        float previousMax = _maxHealth;
        _maxHealthBoostActive = true;
        _maxHealth = _cardMaxHealth * (1f + bonusFraction);
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + (_maxHealth - previousMax));
        PushNetworkState();
    }

    public void ClearMaxHealthBoost()
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        if (!_maxHealthBoostActive)
        {
            return;
        }

        _maxHealthBoostActive = false;
        _maxHealth = _cardMaxHealth;
        _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        PushNetworkState();
    }

    void ConfigureRegenDelayFromCard(string cardId)
    {
        _regenDelaySeconds = cardId == "heavy_2" ? CyborgRegenDelaySeconds : DefaultRegenDelaySeconds;
    }

    void TickHealthRegeneration()
    {
        if (_isDead || _currentHealth >= _maxHealth || _maxHealth <= 0f)
        {
            return;
        }

        if (IsBlindnessBlockingRegen())
        {
            return;
        }

        _secondsSinceHealthDamage += Time.deltaTime;

        float regenAmount = 0f;
        if (_secondsSinceHealthDamage >= _regenDelaySeconds)
        {
            regenAmount += _maxHealth * RegenHealthFractionPerSecond * Time.deltaTime;
        }

        if (_abilityRegenFractionPerSecond > 0f)
        {
            regenAmount += _maxHealth * _abilityRegenFractionPerSecond * Time.deltaTime;
        }

        if (regenAmount <= 0f)
        {
            return;
        }

        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + regenAmount);
        PushNetworkState();
    }

    public void ActivateShield(float amount)
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        _shieldHealth = Mathf.Max(0f, amount);
        ShieldActivatedTime = Time.time;
        PushNetworkState();
    }

    public void ClearShield()
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        _shieldHealth = 0f;
        ShieldActivatedTime = -1f;
        PushNetworkState();
    }

    public void TickShield(float decayPerSecond)
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        if (_shieldHealth <= 0f || decayPerSecond <= 0f)
        {
            return;
        }

        _shieldHealth = Mathf.Max(0f, _shieldHealth - (decayPerSecond * Time.deltaTime));
        PushNetworkState();
    }

    /// <summary>
    /// Debug-only: apply damage against a fixed 100 HP pool, trigger blindness, then refill instantly.
    /// </summary>
    public void ApplyDebugDamage(int damage, bool headshot)
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        const float debugMaxHealth = 100f;
        damage = Mathf.Clamp(damage, 1, 99);

        float blindDuration = ProjectileDamage.ComputeBlindnessDuration(damage / debugMaxHealth, headshot);
        if (blindDuration > 0f)
        {
            ApplyBlindnessToOwner(blindDuration);
        }

        _currentHealth = debugMaxHealth;
        PushNetworkState();
    }

    public void KillAndRespawn()
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        if (_isDead)
        {
            return;
        }

        _isDead = true;
        _currentHealth = 0f;
        ClearShield();
        ClearMaxHealthBoost();
        SetAbilityRegeneration(0f);
        PushNetworkState();

        ThirdPersonController controller = GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.HandlePlayerDeath();
        }

        if (IsSpawned && IsServer && !IsOwner)
        {
            RespawnOwnerRpc();
        }
    }

    public void ApplyDamage(float damage, bool headshot, float blindnessMultiplier = 1f, bool applyBlindness = true)
    {
        float blindDuration = ApplyDamageInternal(damage, headshot, blindnessMultiplier);
        if (applyBlindness && blindDuration > 0f)
        {
            ApplyBlindnessToOwner(blindDuration);
        }
    }

    public float ApplyDamageWithoutBlindness(float damage, bool headshot, float blindnessMultiplier = 1f)
    {
        return ApplyDamageInternal(damage, headshot, blindnessMultiplier);
    }

    float ApplyDamageInternal(float damage, bool headshot, float blindnessMultiplier)
    {
        if (IsSpawned && !IsServer)
        {
            return 0f;
        }

        if (_isDead || damage <= 0f || _maxHealth <= 0f)
        {
            return 0f;
        }

        float healthDamage = ExplosiveVestState.ApplyBodyDamageReduction(damage, headshot, gameObject);
        if (_shieldHealth > 0f)
        {
            float absorbed = Mathf.Min(_shieldHealth, healthDamage);
            _shieldHealth -= absorbed;
            healthDamage -= absorbed;
            PushNetworkState();
        }

        if (healthDamage <= 0f)
        {
            return 0f;
        }

        float healthFraction = healthDamage / _maxHealth;
        _currentHealth = Mathf.Max(0f, _currentHealth - healthDamage);
        _secondsSinceHealthDamage = 0f;
        PushNetworkState();

        if (_currentHealth <= 0f)
        {
            GetComponent<ExplosiveVestState>()?.DetonateOnDeath();
            KillAndRespawn();
            return 0f;
        }

        float blindDuration = ProjectileDamage.ComputeBlindnessDuration(healthFraction, headshot);
        return blindDuration * Mathf.Max(0f, blindnessMultiplier);
    }

    void ApplyBlindnessToOwner(float blindDuration)
    {
        if (IsSpawned && IsServer)
        {
            BlindOwnerRpc(blindDuration);
            return;
        }

        PlayerBulletHitFlash.Instance?.Blind(blindDuration);
    }

    [Rpc(SendTo.Owner)]
    void BlindOwnerRpc(float duration)
    {
        PlayerBulletHitFlash.Instance?.Blind(duration);
    }

    [Rpc(SendTo.Owner)]
    void RespawnOwnerRpc()
    {
        GetComponent<ThirdPersonController>()?.HandlePlayerDeath();
    }

    void HandleNetworkFloatChanged(float previous, float current)
    {
        PullNetworkState();
    }

    void HandleNetworkBoolChanged(bool previous, bool current)
    {
        PullNetworkState();
    }

    void PushNetworkState()
    {
        if (!IsSpawned || !IsServer)
        {
            return;
        }

        _networkCurrentHealth.Value = _currentHealth;
        _networkCardMaxHealth.Value = _cardMaxHealth;
        _networkMaxHealth.Value = _maxHealth;
        _networkShieldHealth.Value = _shieldHealth;
        _networkMaxHealthBoostActive.Value = _maxHealthBoostActive;
        _networkIsDead.Value = _isDead;
    }

    void PullNetworkState()
    {
        if (!IsSpawned)
        {
            return;
        }

        _currentHealth = _networkCurrentHealth.Value;
        _cardMaxHealth = _networkCardMaxHealth.Value;
        _maxHealth = _networkMaxHealth.Value;
        _shieldHealth = _networkShieldHealth.Value;
        _maxHealthBoostActive = _networkMaxHealthBoostActive.Value;
        _isDead = _networkIsDead.Value;
    }

    static bool IsBlindnessBlockingRegen()
    {
        var manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening)
        {
            return false;
        }

        return PlayerBulletHitFlash.Instance != null && PlayerBulletHitFlash.Instance.BlocksGameplayInput;
    }
}
