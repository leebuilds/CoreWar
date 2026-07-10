using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Explosive vest equipped by the Kamikaze E ability. Reduces body-shot damage and
/// detonates on death.
/// </summary>
public class ExplosiveVestState : MonoBehaviour
{
    public const float BodyDamageReductionFraction = 0.05f;
    const float DamageRadiusMeters = 10f;
    const float BuildDestroyRadiusMeters = 8f;
    const float MinEdgeDamage = 10f;
    const float MaxCenterDamage = 130f;

    public bool IsEquipped { get; private set; }
    bool _isDetonating;

    public void Equip()
    {
        _isDetonating = false;
        IsEquipped = true;
        ExplosiveVestVisual.ShowOn(transform);
    }

    public void Clear()
    {
        _isDetonating = false;
        IsEquipped = false;
        ExplosiveVestVisual.HideOn(transform);
    }

    public void DetonateOnDeath()
    {
        DetonateEquipped();
    }

    public void DetonateFromBlast()
    {
        DetonateEquipped();
    }

    public static void DetonateEquippedInRadius(Vector3 center, float radiusMeters)
    {
        var detonated = new HashSet<ExplosiveVestState>();
        Collider[] hits = Physics.OverlapSphere(
            center,
            radiusMeters,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            ExplosiveVestState vest = hit.GetComponentInParent<ExplosiveVestState>();
            if (vest == null || !vest.IsEquipped || !detonated.Add(vest))
            {
                continue;
            }

            Vector3 vestCenter = vest.transform.position + Vector3.up;
            if (Vector3.Distance(center, vestCenter) > radiusMeters)
            {
                continue;
            }

            vest.DetonateFromBlast();
        }
    }

    void DetonateEquipped()
    {
        if (!IsEquipped || _isDetonating)
        {
            return;
        }

        _isDetonating = true;
        IsEquipped = false;
        ExplosiveVestVisual.HideOn(transform);

        Vector3 center = transform.position + Vector3.up;
        ExplosionBlastUtility.Detonate(center, VestExplosionProfile());
        KillWearerIfAlive();
        _isDetonating = false;
    }

    void KillWearerIfAlive()
    {
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && health.IsAlive)
        {
            health.KillAndRespawn();
            return;
        }

        ShootingRangeDummy dummy = GetComponent<ShootingRangeDummy>();
        if (dummy != null && !dummy.IsDown)
        {
            dummy.KillFromExplosion();
        }
    }

    public static ExplosionBlastUtility.Profile VestExplosionProfile()
    {
        return new ExplosionBlastUtility.Profile
        {
            damageRadiusMeters = DamageRadiusMeters,
            buildDestroyRadiusMeters = BuildDestroyRadiusMeters,
            minEdgeDamage = MinEdgeDamage,
            maxCenterDamage = MaxCenterDamage,
            falloff = ExplosionBlastUtility.DamageFalloff.Linear
        };
    }

    public static ExplosiveVestState Ensure(GameObject targetRoot)
    {
        if (targetRoot == null)
        {
            return null;
        }

        var vest = targetRoot.GetComponent<ExplosiveVestState>();
        if (vest == null)
        {
            vest = targetRoot.AddComponent<ExplosiveVestState>();
        }

        return vest;
    }

    public static bool TryGetEquipped(GameObject targetRoot, out ExplosiveVestState vest)
    {
        vest = targetRoot != null ? targetRoot.GetComponent<ExplosiveVestState>() : null;
        return vest != null && vest.IsEquipped;
    }

    public static float ApplyBodyDamageReduction(float damage, bool headshot, GameObject targetRoot)
    {
        if (headshot || damage <= 0f || !TryGetEquipped(targetRoot, out _))
        {
            return damage;
        }

        return damage * (1f - BodyDamageReductionFraction);
    }
}
