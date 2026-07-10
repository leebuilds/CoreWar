using UnityEngine;

/// <summary>
/// Thrown flashbang grenade projectile.
/// </summary>
public class FlashbangGrenadeProjectile : ThrownGrenadeProjectile
{
    static readonly Color GrenadeColor = new Color(0.34f, 0.35f, 0.36f, 1f);

    protected override GrenadeType GrenadeType => global::GrenadeType.Flashbang;
    protected override Color BodyColor => GrenadeColor;
    protected override float BodyMetallic => 0.34f;
    protected override float BodyGlossiness => 0.42f;
    protected override string VisualObjectName => "Flashbang Visual";

    protected override void DetonateAt(Vector3 center)
    {
        FlashbangBlindUtility.DetonateFlashbang(center);
    }
}
