using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Frag grenade damage with line-of-sight checks and gun-style blindness.
/// </summary>
public static class GrenadeBlastUtility
{
    public const float FragDamageRadiusMeters = 8f;
    public const float FragMaxCenterDamage = 70f;
    public const float FragMinEdgeDamage = 15f;

    public static void DetonateFrag(Vector3 center)
    {
        ApplyFragDamage(center);
        FragGrenadeSmokeEffect.Spawn(center);
    }

    public static void ApplyFragDamage(Vector3 center)
    {
        var damagedRoots = new HashSet<GameObject>();
        Collider[] hits = Physics.OverlapSphere(
            center,
            FragDamageRadiusMeters,
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

                TryApplyFragDamage(center, dummy.HeadMarkCenter, dummy.gameObject, damageTarget =>
                {
                    dummy.ApplyDirectDamage(damageTarget, false);
                });
                continue;
            }

            var controller = hit.GetComponentInParent<ThirdPersonController>();
            if (controller == null || !damagedRoots.Add(controller.gameObject))
            {
                continue;
            }

            Vector3 targetPoint = controller.transform.position + Vector3.up;
            TryApplyFragDamage(center, targetPoint, controller.gameObject, damage =>
            {
                var health = controller.GetComponent<PlayerHealth>();
                health?.ApplyDamage(damage, false);
            });
        }
    }

    static void TryApplyFragDamage(Vector3 center, Vector3 targetPoint, GameObject targetRoot, System.Action<float> apply)
    {
        float distance = Vector3.Distance(center, targetPoint);
        if (distance >= FragDamageRadiusMeters)
        {
            return;
        }

        if (!HasLineOfSight(center, targetPoint, targetRoot))
        {
            return;
        }

        float damage = DamageAtDistance(distance);
        if (damage > 0f)
        {
            apply(damage);
        }
    }

    public static float DamageAtDistance(float distanceMeters)
    {
        if (distanceMeters >= FragDamageRadiusMeters)
        {
            return 0f;
        }

        float t = Mathf.Clamp01(distanceMeters / FragDamageRadiusMeters);
        return Mathf.Lerp(FragMaxCenterDamage, FragMinEdgeDamage, t);
    }

    static bool HasLineOfSight(Vector3 from, Vector3 to, GameObject targetRoot)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
        {
            return true;
        }

        if (!Physics.Raycast(
                from,
                direction.normalized,
                out RaycastHit hit,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return targetRoot != null &&
            hit.collider != null &&
            hit.collider.transform.IsChildOf(targetRoot.transform);
    }
}
