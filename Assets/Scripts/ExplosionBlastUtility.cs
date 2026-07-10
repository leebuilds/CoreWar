using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared explosion damage, fiery blindness, build destruction, and VFX.
/// </summary>
public static class ExplosionBlastUtility
{
    public const float ExplosionBlindnessMultiplier = 2f;

    public enum DamageFalloff
    {
        Exponential,
        Linear
    }

    public struct Profile
    {
        public float damageRadiusMeters;
        public float buildDestroyRadiusMeters;
        public float minEdgeDamage;
        public float maxCenterDamage;
        public DamageFalloff falloff;
    }

    public static void Detonate(Vector3 center, Profile profile)
    {
        ApplyExplosionDamage(center, profile);
        ExplosiveVestState.DetonateEquippedInRadius(center, profile.damageRadiusMeters);
        DestroyBuildPiecesNear(center, profile.buildDestroyRadiusMeters);
        AntiMaterialExplosionEffect.Spawn(center);
    }

    public static void ApplyExplosionDamage(Vector3 center, Profile profile)
    {
        var damagedRoots = new HashSet<GameObject>();
        Collider[] hits = Physics.OverlapSphere(
            center,
            profile.damageRadiusMeters,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            var dummy = hit.GetComponentInParent<ShootingRangeDummy>();
            if (dummy != null)
            {
                if (!damagedRoots.Add(dummy.gameObject))
                {
                    continue;
                }

                float damage = DamageAtDistance(Vector3.Distance(center, dummy.transform.position), profile);
                if (damage > 0f)
                {
                    dummy.ApplyDirectDamage(damage, false);
                }

                continue;
            }

            var controller = hit.GetComponentInParent<ThirdPersonController>();
            if (controller == null || !damagedRoots.Add(controller.gameObject))
            {
                continue;
            }

            float playerDistance = Vector3.Distance(center, controller.transform.position);
            float playerDamage = DamageAtDistance(playerDistance, profile);
            bool inFire = playerDistance <= AntiMaterialExplosionEffect.FireRadiusMeters;
            var health = controller.GetComponent<PlayerHealth>();
            if (health == null)
            {
                continue;
            }

            float blindDuration = 0f;
            if (playerDamage > 0f)
            {
                blindDuration = health.ApplyDamageWithoutBlindness(
                    playerDamage,
                    false,
                    ExplosionBlindnessMultiplier);
            }

            if (controller == ThirdPersonController.Local && (inFire || blindDuration > 0f))
            {
                PlayerBulletHitFlash.Instance?.BlindFromExplosionFire(blindDuration, inFire);
            }
        }

        C4ChargeProjectile.ApplyBlastDamage(
            center,
            profile.damageRadiusMeters,
            distance => DamageAtDistance(distance, profile));
    }

    public static float DamageAtDistance(float distanceMeters, Profile profile)
    {
        if (distanceMeters >= profile.damageRadiusMeters)
        {
            return 0f;
        }

        switch (profile.falloff)
        {
            case DamageFalloff.Linear:
            {
                float t = Mathf.Clamp01(distanceMeters / profile.damageRadiusMeters);
                return Mathf.Lerp(profile.maxCenterDamage, profile.minEdgeDamage, t);
            }
            default:
            {
                float closeness = 1f - (distanceMeters / profile.damageRadiusMeters);
                return profile.minEdgeDamage *
                    Mathf.Pow(profile.maxCenterDamage / profile.minEdgeDamage, closeness);
            }
        }
    }

    static void DestroyBuildPiecesNear(Vector3 center, float radiusMeters)
    {
        var world = Object.FindFirstObjectByType<VoxelLightingWorld>();
        if (world == null)
        {
            return;
        }

        var removed = new HashSet<PlayerBuiltVoxel>();
        Collider[] hits = Physics.OverlapSphere(
            center,
            radiusMeters,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            var marker = hits[i].GetComponentInParent<PlayerBuiltVoxel>();
            if (marker == null || !removed.Add(marker))
            {
                continue;
            }

            world.TryRemovePlayerBuiltObject(marker);
        }
    }
}
