using UnityEngine;

/// <summary>
/// Flashbang blindness: 150-degree view cone, 30 m range, white fade by distance.
/// </summary>
public static class FlashbangBlindUtility
{
    public const float MaxRangeMeters = 30f;
    public const float StrongRangeMeters = 15f;
    public const float TemporaryBlindnessSeconds = 4f;
    public const float MaxCompleteWhiteSeconds = 4f;
    public const float ViewConeDegrees = 150f;
    const float MinPeakAlpha = 0.20f;
    const float MaxPeakAlpha = 1f;

    public static void DetonateFlashbang(Vector3 center)
    {
        FlashbangBurstEffect.Spawn(center);
        ApplyBlindnessToViewers(center);
    }

    static void ApplyBlindnessToViewers(Vector3 center)
    {
        var flash = PlayerBulletHitFlash.Instance ?? PlayerBulletHitFlash.Create();
        var viewers = Object.FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None);
        for (int i = 0; i < viewers.Length; i++)
        {
            ThirdPersonController viewer = viewers[i];
            if (viewer == null || !viewer.isActiveAndEnabled)
            {
                continue;
            }

            if (viewer != ThirdPersonController.Local)
            {
                continue;
            }

            if (!TryComputeEffect(
                    viewer,
                    center,
                    out float completeWhiteSeconds,
                    out float fadePeakAlpha))
            {
                continue;
            }

            flash.BlindFromFlashbang(completeWhiteSeconds, TemporaryBlindnessSeconds, fadePeakAlpha);
        }
    }

    public static bool TryComputeEffect(
        ThirdPersonController viewer,
        Vector3 flashCenter,
        out float completeWhiteSeconds,
        out float fadePeakAlpha)
    {
        completeWhiteSeconds = 0f;
        fadePeakAlpha = 0f;

        Camera camera = viewer.viewCamera;
        if (camera == null)
        {
            return false;
        }

        Vector3 eyePosition = camera.transform.position;
        Vector3 toFlash = flashCenter - eyePosition;
        float distance = toFlash.magnitude;
        if (distance > MaxRangeMeters)
        {
            return false;
        }

        if (distance <= 0.05f)
        {
            toFlash = camera.transform.forward;
            distance = 0f;
        }
        else
        {
            toFlash /= distance;
        }

        if (!IsWithinViewCone(camera, toFlash))
        {
            return false;
        }

        if (!HasLineOfSightToFlash(eyePosition, flashCenter, viewer.gameObject))
        {
            return false;
        }

        float alphaBlend = Mathf.InverseLerp(MaxRangeMeters, StrongRangeMeters, distance);
        fadePeakAlpha = Mathf.Lerp(MinPeakAlpha, MaxPeakAlpha, alphaBlend);

        completeWhiteSeconds = distance < StrongRangeMeters
            ? Mathf.Lerp(MaxCompleteWhiteSeconds, 0f, distance / StrongRangeMeters)
            : 0f;

        return fadePeakAlpha > 0f;
    }

    static bool IsWithinViewCone(Camera camera, Vector3 directionToFlash)
    {
        float halfCone = ViewConeDegrees * 0.5f;
        float viewAngle = Vector3.Angle(camera.transform.forward, directionToFlash);
        return viewAngle <= halfCone;
    }

    static bool HasLineOfSightToFlash(Vector3 from, Vector3 flashCenter, GameObject viewerRoot)
    {
        Vector3 direction = flashCenter - from;
        float distance = direction.magnitude;
        if (distance <= 0.05f)
        {
            return true;
        }

        Vector3 normalized = direction / distance;
        if (!Physics.Raycast(
                from,
                normalized,
                out RaycastHit hit,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        if (viewerRoot != null &&
            hit.collider != null &&
            hit.collider.transform.IsChildOf(viewerRoot.transform))
        {
            return true;
        }

        return Vector3.Distance(hit.point, flashCenter) <= 1.25f;
    }
}
