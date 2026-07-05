using UnityEngine;

public enum ShootingRangeHitZoneType
{
    Body,
    Head
}

/// <summary>
/// Marks colliders on shooting range dummies for bullet hit resolution.
/// </summary>
public class ShootingRangeHitZone : MonoBehaviour
{
    public ShootingRangeHitZoneType zoneType = ShootingRangeHitZoneType.Body;
    public ShootingRangeDummy dummy;
}
