using UnityEngine;

/// <summary>
/// Local player health from the active class card. Damage triggers blindness only for now.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public const float BaselineMaxHealth = 100f;

    float _currentHealth;
    float _maxHealth;
    float _shieldHealth;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public float ShieldHealth => _shieldHealth;
    public bool HasShield => _shieldHealth > 0f;
    public float ShieldActivatedTime { get; private set; } = -1f;
    public float HealthFraction => _maxHealth > 0f ? Mathf.Clamp01(_currentHealth / _maxHealth) : 0f;
    public bool UsesModifiedMaxHealth => Mathf.Abs(_maxHealth - BaselineMaxHealth) > 0.01f;

    void Start()
    {
        RefillHealth();
    }

    public void RefillHealth()
    {
        var card = CardCatalog.Get(GameSession.ActiveCardId);
        _maxHealth = Mathf.Max(1f, card?.preview.health ?? BaselineMaxHealth);
        _currentHealth = _maxHealth;
        _shieldHealth = 0f;
        ShieldActivatedTime = -1f;
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

    public void ApplyDamage(float damage, bool headshot)
    {
        if (damage <= 0f || _maxHealth <= 0f)
        {
            return;
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
            return;
        }

        float healthFraction = healthDamage / _maxHealth;
        _currentHealth = Mathf.Max(0f, _currentHealth - healthDamage);

        float blindDuration = ProjectileDamage.ComputeBlindnessDuration(healthFraction, headshot);
        if (blindDuration > 0f)
        {
            PlayerBulletHitFlash.Instance?.Blind(blindDuration);
        }
    }
}
