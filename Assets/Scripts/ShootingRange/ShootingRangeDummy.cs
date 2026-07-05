using System.Collections;
using UnityEngine;

/// <summary>
/// Humanoid target dummy with configurable health and head/body hit zones.
/// </summary>
public class ShootingRangeDummy : MonoBehaviour
{
    const int BodyDamage = 30;
    const int HeadDamage = 60;
    const float RespawnDelaySeconds = 3f;

    ShootingRangeHitZone _headZone;
    ShootingRangeHitZone _bodyZone;
    CapsuleRobotVisual _visual;
    Renderer[] _flashRenderers;
    Texture[] _defaultAlbedo;
    float _currentHealth;
    bool _isDown;
    Coroutine _flashRoutine;
    Coroutine _respawnRoutine;
    Vector3 _standPosition;
    Quaternion _standRotation;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => ShootingRangeSession.DummyMaxHealth;
    public bool IsDown => _isDown;

    public static ShootingRangeDummy Create(Transform parent, Vector3 worldPosition, int distanceMeters,
        Quaternion rotation)
    {
        var root = new GameObject($"Dummy {distanceMeters}m");
        root.transform.SetParent(parent, false);
        root.transform.position = worldPosition;
        root.transform.rotation = rotation;

        var dummy = root.AddComponent<ShootingRangeDummy>();
        dummy.Build(distanceMeters);
        ShootingRangeSession.RegisterDummy(dummy);
        return dummy;
    }

    void Build(int distanceMeters)
    {
        _standPosition = transform.position;
        _standRotation = transform.rotation;

        var visualRoot = new GameObject("Dummy Visual");
        visualRoot.transform.SetParent(transform, false);
        _visual = visualRoot.AddComponent<CapsuleRobotVisual>();
        _visual.BuildNeutralDummy();

        _bodyZone = AddHitZone(visualRoot.transform, "Body Hit Zone",
            new Vector3(0f, 1.05f, 0f), new Vector3(0.55f, 0.7f, 0.45f), ShootingRangeHitZoneType.Body);
        _headZone = AddHitZone(visualRoot.transform, "Head Hit Zone",
            new Vector3(0f, 1.52f, 0f), new Vector3(0.36f, 0.28f, 0.36f), ShootingRangeHitZoneType.Head);

        CacheFlashRenderers();
        RefillHealth();
    }

    void CacheFlashRenderers()
    {
        _flashRenderers = GetComponentsInChildren<Renderer>();
        _defaultAlbedo = new Texture[_flashRenderers.Length];
        for (int i = 0; i < _flashRenderers.Length; i++)
        {
            _defaultAlbedo[i] = VoxelMaterialUtility.GetRendererAlbedo(_flashRenderers[i]);
        }
    }

    ShootingRangeHitZone AddHitZone(Transform parent, string name, Vector3 localCenter,
        Vector3 size, ShootingRangeHitZoneType zoneType)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localCenter;
        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        var zone = go.AddComponent<ShootingRangeHitZone>();
        zone.zoneType = zoneType;
        zone.dummy = this;
        return zone;
    }

    public void RefillHealth()
    {
        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
            _respawnRoutine = null;
        }

        _currentHealth = ShootingRangeSession.DummyMaxHealth;
        _isDown = false;
        transform.position = _standPosition;
        transform.rotation = _standRotation;
        SetVisualActive(true);
    }

    public bool ApplyHit(ShootingRangeHitZoneType zoneType)
    {
        if (_isDown)
        {
            return false;
        }

        int damage = zoneType == ShootingRangeHitZoneType.Head ? HeadDamage : BodyDamage;
        bool headshot = zoneType == ShootingRangeHitZoneType.Head;
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        MenuUiSounds.PlayRangeDing(headshot);
        FlashHit(headshot);

        if (_currentHealth <= 0f)
        {
            _isDown = true;
            SetVisualActive(false);
            _respawnRoutine = StartCoroutine(RespawnAfterDelay());
        }

        return true;
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(RespawnDelaySeconds);
        _respawnRoutine = null;
        RefillHealth();
    }

    void FlashHit(bool headshot)
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
        }

        _flashRoutine = StartCoroutine(FlashRoutine(headshot));
    }

    IEnumerator FlashRoutine(bool headshot)
    {
        var flash = headshot ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 0.75f, 0.35f);
        var flashTexture = VoxelMaterialUtility.GetSolidTexture(flash);

        for (int i = 0; i < _flashRenderers.Length; i++)
        {
            if (_flashRenderers[i] != null)
            {
                VoxelMaterialUtility.SetRendererAlbedoTexture(_flashRenderers[i], flashTexture);
            }
        }

        yield return new WaitForSeconds(0.12f);

        for (int i = 0; i < _flashRenderers.Length; i++)
        {
            if (_flashRenderers[i] != null)
            {
                VoxelMaterialUtility.SetRendererAlbedoTexture(_flashRenderers[i], _defaultAlbedo[i]);
            }
        }

        _flashRoutine = null;
    }

    void SetVisualActive(bool active)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = active;
        }

        if (_headZone != null)
        {
            _headZone.GetComponent<Collider>().enabled = active;
        }

        if (_bodyZone != null)
        {
            _bodyZone.GetComponent<Collider>().enabled = active;
        }
    }
}
