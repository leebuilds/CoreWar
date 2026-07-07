using UnityEngine;

/// <summary>
/// Local player health from the active class card. Damage triggers blindness only for now.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    float _currentHealth;
    float _maxHealth;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;

    void Start()
    {
        RefillHealth();
    }

    public void RefillHealth()
    {
        var card = CardCatalog.Get(GameSession.ActiveCardId);
        _maxHealth = Mathf.Max(1f, card?.preview.health ?? 100);
        _currentHealth = _maxHealth;
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

        float healthFraction = damage / _maxHealth;
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);

        float blindDuration = ProjectileDamage.ComputeBlindnessDuration(healthFraction, headshot);
        if (blindDuration > 0f)
        {
            PlayerBulletHitFlash.Instance?.Blind(blindDuration);
        }
    }
}
