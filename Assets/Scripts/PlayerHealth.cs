using UnityEngine;

/// <summary>
/// Local player health from the active class card. Damage triggers blindness only for now.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public const float BaselineMaxHealth = 100f;
    public const float DefaultRegenDelaySeconds = 4f;
    public const float CyborgRegenDelaySeconds = 2f;
    public const float RegenHealthFractionPerSecond = 0.1f;
    public const float CyborgMaxHealthBoostFraction = 0.15f;

    float _currentHealth;
    float _cardMaxHealth;
    float _maxHealth;
    float _shieldHealth;
    float _secondsSinceHealthDamage;
    float _regenDelaySeconds = DefaultRegenDelaySeconds;
    float _abilityRegenFractionPerSecond;
    bool _maxHealthBoostActive;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public float BaseMaxHealth => _cardMaxHealth;
    public float ShieldHealth => _shieldHealth;
    public bool HasShield => _shieldHealth > 0f;
    public bool HasMaxHealthBoost => _maxHealthBoostActive;
    public float ShieldActivatedTime { get; private set; } = -1f;
    public float HealthFraction => _maxHealth > 0f ? Mathf.Clamp01(_currentHealth / _maxHealth) : 0f;
    public bool UsesModifiedMaxHealth => Mathf.Abs(_maxHealth - BaselineMaxHealth) > 0.01f;
    public bool IsRegenerating =>
        _currentHealth < _maxHealth &&
        !IsBlindnessBlockingRegen() &&
        (_secondsSinceHealthDamage >= _regenDelaySeconds || _abilityRegenFractionPerSecond > 0f);

    void Start()
    {
        RefillHealth();
    }

    void Update()
    {
        TickHealthRegeneration();
    }

    public void RefillHealth()
    {
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
    }

    public void SetAbilityRegeneration(float fractionPerSecond)
    {
        _abilityRegenFractionPerSecond = Mathf.Max(0f, fractionPerSecond);
    }

    public void ActivateMaxHealthBoost(float bonusFraction)
    {
        bonusFraction = Mathf.Max(0f, bonusFraction);
        if (_maxHealthBoostActive || bonusFraction <= 0f)
        {
            return;
        }

        float previousMax = _maxHealth;
        _maxHealthBoostActive = true;
        _maxHealth = _cardMaxHealth * (1f + bonusFraction);
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + (_maxHealth - previousMax));
    }

    public void ClearMaxHealthBoost()
    {
        if (!_maxHealthBoostActive)
        {
            return;
        }

        _maxHealthBoostActive = false;
        _maxHealth = _cardMaxHealth;
        _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
    }

    void ConfigureRegenDelayFromCard(string cardId)
    {
        _regenDelaySeconds = cardId == "heavy_2" ? CyborgRegenDelaySeconds : DefaultRegenDelaySeconds;
    }

    void TickHealthRegeneration()
    {
        if (_currentHealth >= _maxHealth || _maxHealth <= 0f)
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
    }

    public void ActivateShield(float amount)
    {
        _shieldHealth = Mathf.Max(0f, amount);
        ShieldActivatedTime = Time.time;
    }

    public void ClearShield()
    {
        _shieldHealth = 0f;
        ShieldActivatedTime = -1f;
    }

    public void TickShield(float decayPerSecond)
    {
        if (_shieldHealth <= 0f || decayPerSecond <= 0f)
        {
            return;
        }

        _shieldHealth = Mathf.Max(0f, _shieldHealth - (decayPerSecond * Time.deltaTime));
    }

    /// <summary>
    /// Debug-only: apply damage against a fixed 100 HP pool, trigger blindness, then refill instantly.
    /// </summary>
    public void ApplyDebugDamage(int damage, bool headshot)
    {
        const float debugMaxHealth = 100f;
        damage = Mathf.Clamp(damage, 1, 99);

        float blindDuration = ProjectileDamage.ComputeBlindnessDuration(damage / debugMaxHealth, headshot);
        if (blindDuration > 0f)
        {
            PlayerBulletHitFlash.Instance?.Blind(blindDuration);
        }

        _currentHealth = debugMaxHealth;
    }

    public void ApplyDamage(float damage, bool headshot, float blindnessMultiplier = 1f, bool applyBlindness = true)
    {
        float blindDuration = ApplyDamageInternal(damage, headshot, blindnessMultiplier);
        if (applyBlindness && blindDuration > 0f)
        {
            PlayerBulletHitFlash.Instance?.Blind(blindDuration);
        }
    }

    public float ApplyDamageWithoutBlindness(float damage, bool headshot, float blindnessMultiplier = 1f)
    {
        return ApplyDamageInternal(damage, headshot, blindnessMultiplier);
    }

    float ApplyDamageInternal(float damage, bool headshot, float blindnessMultiplier)
    {
        if (damage <= 0f || _maxHealth <= 0f)
        {
            return 0f;
        }

        float healthDamage = damage;
        if (_shieldHealth > 0f)
        {
            float absorbed = Mathf.Min(_shieldHealth, healthDamage);
            _shieldHealth -= absorbed;
            healthDamage -= absorbed;
        }

        if (healthDamage <= 0f)
        {
            return 0f;
        }

        float healthFraction = healthDamage / _maxHealth;
        _currentHealth = Mathf.Max(0f, _currentHealth - healthDamage);
        _secondsSinceHealthDamage = 0f;

        float blindDuration = ProjectileDamage.ComputeBlindnessDuration(healthFraction, headshot);
        return blindDuration * Mathf.Max(0f, blindnessMultiplier);
    }

    static bool IsBlindnessBlockingRegen()
    {
        return PlayerBulletHitFlash.Instance != null && PlayerBulletHitFlash.Instance.IsBlind;
    }
}
