using UnityEngine;

/// <summary>
/// Shared machine gun suppression timer and movement slow (non-stacking speed penalty).
/// </summary>
public static class MachineGunSuppressionUtility
{
    public const float DefaultDurationSeconds = 1.3f;
    public const float DefaultSpeedMultiplier = 0.8f;
    public const float BoostedSpeedMultiplier = 0.65f;
    public const float BoostedFlickIntensityMultiplier = 2.5f;

    public static void Apply(ref float remainingSeconds, float durationSeconds)
    {
        remainingSeconds = durationSeconds;
    }

    public static void ApplySpeedMultiplier(
        ref float activeSpeedMultiplier,
        float incomingSpeedMultiplier,
        bool wasAlreadySuppressed)
    {
        if (!wasAlreadySuppressed)
        {
            activeSpeedMultiplier = incomingSpeedMultiplier;
            return;
        }

        activeSpeedMultiplier = Mathf.Min(activeSpeedMultiplier, incomingSpeedMultiplier);
    }

    public static void Tick(ref float remainingSeconds, float deltaTime)
    {
        if (remainingSeconds <= 0f)
        {
            return;
        }

        remainingSeconds = Mathf.Max(0f, remainingSeconds - deltaTime);
    }

    public static float SpeedFactor(float remainingSeconds, float speedMultiplier)
    {
        return remainingSeconds > 0f ? speedMultiplier : 1f;
    }
}
