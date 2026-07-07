using UnityEngine;

/// <summary>
/// Shared bullet damage and player blindness tuning.
/// </summary>
public static class ProjectileDamage
{
    public const float MinSpeedForDamage = 25f;
    public const float PointBlankBodyDamage = 40f;
    public const float HeadshotMultiplier = 2f;

    const float BlindnessHealthFractionLo = 0.5f;
    const float BlindnessDurationLo = 0.125f;
    const float BlindnessHealthFractionHi = 0.99f;
    const float BlindnessDurationHi = 2f;
    const float BlindnessCurveExponent = 2.2f;

    public static float ComputeDamage(float impactSpeed, float muzzleSpeed, bool headshot)
    {
        if (impactSpeed < MinSpeedForDamage || muzzleSpeed <= 0.0001f)
        {
            return 0f;
        }

        float damage = (impactSpeed / muzzleSpeed) * PointBlankBodyDamage;
        if (headshot)
        {
            damage *= HeadshotMultiplier;
        }

        return damage;
    }

    /// <summary>
    /// Blindness seconds from damage as a fraction of max health (0–1).
    /// 50% → 0.125 s, 99% → 2 s (log, raised to 2.2); below 50% scales linearly to zero.
    /// Headshots double the result.
    /// </summary>
    public static float ComputeBlindnessDuration(float healthDamageFraction, bool headshot)
    {
        healthDamageFraction = Mathf.Clamp01(healthDamageFraction);
        if (healthDamageFraction <= 0f)
        {
            return 0f;
        }

        float duration;
        if (healthDamageFraction < BlindnessHealthFractionLo)
        {
            duration = (healthDamageFraction / BlindnessHealthFractionLo) * BlindnessDurationLo;
        }
        else
        {
            float fraction = Mathf.Min(healthDamageFraction, BlindnessHealthFractionHi);
            float logSpan = Mathf.Log(BlindnessHealthFractionHi) - Mathf.Log(BlindnessHealthFractionLo);
            float u = (Mathf.Log(fraction) - Mathf.Log(BlindnessHealthFractionLo)) / logSpan;
            u = Mathf.Pow(Mathf.Clamp01(u), BlindnessCurveExponent);
            duration = BlindnessDurationLo + (u * (BlindnessDurationHi - BlindnessDurationLo));
        }

        if (headshot)
        {
            duration *= HeadshotMultiplier;
        }

        return duration;
    }
}
