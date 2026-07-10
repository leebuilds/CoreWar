using UnityEngine;

/// <summary>
/// Thrown frag grenade projectile.
/// </summary>
public class FragGrenadeProjectile : ThrownGrenadeProjectile
{
    static readonly Color GrenadeColor = new Color(0.42f, 0.44f, 0.46f, 1f);

    protected override GrenadeType GrenadeType => global::GrenadeType.Frag;
    protected override Color BodyColor => GrenadeColor;
    protected override float BodyMetallic => 0.58f;
    protected override float BodyGlossiness => 0.74f;
    protected override string VisualObjectName => "Frag Grenade Visual";

    protected override void DetonateAt(Vector3 center)
    {
        GrenadeBlastUtility.DetonateFrag(center);
    }
}
