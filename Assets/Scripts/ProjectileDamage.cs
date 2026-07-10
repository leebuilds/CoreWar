using UnityEngine;

public enum ProjectileWeaponType
{
    AssaultRifle,
    Pistol,
    SniperRifle,
    HuntingRifle,
    Smg,
    MachinePistol,
    LightMachineGun,
    MachineGun,
    CyborgLaser,
    AntiMaterialRifle
}

/// <summary>
/// Per-weapon velocity-scaled bullet damage and player blindness tuning.
/// </summary>
public static class ProjectileDamage
{
    public const float HeadshotBlindnessMultiplier = 2f;
    public const float MinSpeedDamageFraction = 0.5f;
    public const float PlayerBounceMinSpeed = 30f;

    const float BlindnessHealthFractionLo = 0.5f;
    const float BlindnessDurationLo = 0.125f;
    const float BlindnessHealthFractionHi = 0.99f;
    const float BlindnessDurationHi = 2f;
    const float BlindnessCurveExponent = 2.2f;

    public static float ComputeDamage(
        float impactSpeed,
        float muzzleSpeed,
        ProjectileWeaponType weaponType,
        bool headshot)
    {
        if (muzzleSpeed <= 0.0001f)
        {
            return 0f;
        }

        float speedRatio = impactSpeed <= 0f ? 0f : Mathf.Clamp01(impactSpeed / muzzleSpeed);
        float maxDamage = headshot
            ? MaxHeadshotDamage(weaponType)
            : MaxBodyDamage(weaponType);
        return maxDamage * Mathf.Lerp(MinSpeedDamageFraction, 1f, speedRatio);
    }

    public static float MaxBodyDamage(ProjectileWeaponType weaponType)
    {
        switch (weaponType)
        {
            case ProjectileWeaponType.SniperRifle:
                return 80f;
            case ProjectileWeaponType.HuntingRifle:
                return 65f;
            case ProjectileWeaponType.LightMachineGun:
                return 20f;
            case ProjectileWeaponType.MachineGun:
                return 7f;
            case ProjectileWeaponType.CyborgLaser:
                return 10f;
            case ProjectileWeaponType.AntiMaterialRifle:
                return 90f;
            case ProjectileWeaponType.Smg:
                return 16f;
            case ProjectileWeaponType.MachinePistol:
                return 13f;
            case ProjectileWeaponType.Pistol:
                return 13f;
            case ProjectileWeaponType.AssaultRifle:
            default:
                return 17f;
        }
    }

    public static float MaxHeadshotDamage(ProjectileWeaponType weaponType)
    {
        switch (weaponType)
        {
            case ProjectileWeaponType.SniperRifle:
                return 100f;
            case ProjectileWeaponType.HuntingRifle:
                return 160f;
            case ProjectileWeaponType.LightMachineGun:
                return 24f;
            case ProjectileWeaponType.MachineGun:
                return 9f;
            case ProjectileWeaponType.CyborgLaser:
                return 15f;
            case ProjectileWeaponType.AntiMaterialRifle:
                return 102f;
            case ProjectileWeaponType.Smg:
                return 28f;
            case ProjectileWeaponType.MachinePistol:
                return 26f;
            case ProjectileWeaponType.Pistol:
                return 26f;
            case ProjectileWeaponType.AssaultRifle:
            default:
                return 23f;
        }
    }

    /// <summary>
    /// Fraction of speed retained after 100 m of air travel (exponential decay).
    /// Pistol ~25% loss, AR ~5%, sniper ~2%.
    /// </summary>
    public static float AirSpeedRetentionPer100Meters(ProjectileWeaponType weaponType)
    {
        switch (weaponType)
        {
            case ProjectileWeaponType.Pistol:
                return 0.75f;
            case ProjectileWeaponType.Smg:
                return 0.935f;
            case ProjectileWeaponType.MachinePistol:
                return 0.5f;
            case ProjectileWeaponType.AssaultRifle:
                return 0.95f;
            case ProjectileWeaponType.LightMachineGun:
                return 0.945f;
            case ProjectileWeaponType.MachineGun:
                return 0.76f;
            case ProjectileWeaponType.CyborgLaser:
                return 1f;
            case ProjectileWeaponType.AntiMaterialRifle:
                return 0.97f;
            case ProjectileWeaponType.SniperRifle:
                return 0.98f;
            case ProjectileWeaponType.HuntingRifle:
                return 0.96f;
            default:
                return 0.95f;
        }
    }

    public static float AirDragPerMeter(ProjectileWeaponType weaponType)
    {
        float retention = Mathf.Clamp(AirSpeedRetentionPer100Meters(weaponType), 0.01f, 0.9999f);
        return -Mathf.Log(retention) / 100f;
    }

    public static void ApplyAirDrag(ref Vector3 velocity, ProjectileWeaponType weaponType, float distanceMeters)
    {
        if (distanceMeters <= 0f || velocity.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float drag = Mathf.Exp(-AirDragPerMeter(weaponType) * distanceMeters);
        velocity *= drag;
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
            duration *= HeadshotBlindnessMultiplier;
        }

        return duration;
    }
}
