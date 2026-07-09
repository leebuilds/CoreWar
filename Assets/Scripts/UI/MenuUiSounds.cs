using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Procedural UI and menu sound effects (no audio assets required).
/// </summary>
public static class MenuUiSounds
{
    static AudioSource _source;
    static float _lastHoverTime;
    static AudioClip _hoverClip;
    static AudioClip _clickClip;
    static AudioClip _gunshotClip;

    public static void EnsureInitialized()
    {
        if (_source != null)
        {
            return;
        }

        var go = new GameObject("Menu UI Audio");
        Object.DontDestroyOnLoad(go);
        _source = go.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
        _source.volume = 0.55f;
    }

    public static void ApplySettings()
    {
        EnsureInitialized();
        _source.volume = MenuSettings.UiSoundsEnabled ? MenuSettings.MasterVolume : 0f;
    }

    public static void PlayHover()
    {
        EnsureInitialized();
        if (!MenuSettings.UiSoundsEnabled)
        {
            return;
        }
        if (Time.unscaledTime - _lastHoverTime < 0.05f)
        {
            return;
        }

        _lastHoverTime = Time.unscaledTime;
        _source.pitch = Random.Range(0.95f, 1.05f);
        if (_hoverClip == null)
        {
            _hoverClip = CreateClickClip(880f, 0.035f, 0.18f);
        }

        _source.PlayOneShot(_hoverClip);
    }

    public static void PlayClick()
    {
        EnsureInitialized();
        if (!MenuSettings.UiSoundsEnabled)
        {
            return;
        }
        _source.pitch = 1f;
        if (_clickClip == null)
        {
            _clickClip = CreateClickClip(620f, 0.05f, 0.28f);
        }

        _source.PlayOneShot(_clickClip);
    }

    public static void PlayGunshot()
    {
        PlayWeaponGunshot(ProjectileWeaponType.Pistol);
    }

    public static void PlayWeaponGunshot(ProjectileWeaponType weaponType)
    {
        EnsureInitialized();
        if (!MenuSettings.UiSoundsEnabled)
        {
            return;
        }

        float pitch = weaponType switch
        {
            ProjectileWeaponType.AssaultRifle => Random.Range(0.82f, 0.9f),
            ProjectileWeaponType.LightMachineGun => Random.Range(0.76f, 0.84f),
            ProjectileWeaponType.Smg => Random.Range(0.96f, 1.06f),
            ProjectileWeaponType.MachinePistol => Random.Range(0.96f, 1.06f),
            ProjectileWeaponType.SniperRifle => Random.Range(0.68f, 0.76f),
            ProjectileWeaponType.HuntingRifle => Random.Range(0.7f, 0.78f),
            ProjectileWeaponType.CyborgLaser => Random.Range(1.18f, 1.28f),
            _ => Random.Range(0.98f, 1.08f)
        };

        _source.pitch = pitch;
        if (_gunshotClip == null)
        {
            _gunshotClip = CreateGunshotClip();
        }

        _source.PlayOneShot(_gunshotClip, weaponType switch
        {
            ProjectileWeaponType.SniperRifle => 0.62f,
            ProjectileWeaponType.AntiMaterialRifle => 0.72f,
            ProjectileWeaponType.CyborgLaser => 0.34f,
            _ => 0.48f
        });
    }

    static AudioClip _antiMaterialChargeClip;
    static bool _antiMaterialChargePlaying;

    public static void StartAntiMaterialCharge()
    {
        EnsureInitialized();
        if (!MenuSettings.UiSoundsEnabled)
        {
            return;
        }

        if (_antiMaterialChargeClip == null)
        {
            _antiMaterialChargeClip = CreateAntiMaterialChargeClip();
        }

        _source.loop = true;
        _source.clip = _antiMaterialChargeClip;
        _source.pitch = 0.65f;
        _source.volume = MenuSettings.UiSoundsEnabled ? MenuSettings.MasterVolume * 0.42f : 0f;
        _source.Play();
        _antiMaterialChargePlaying = true;
    }

    public static void UpdateAntiMaterialCharge(float progress)
    {
        if (!_antiMaterialChargePlaying || _source == null)
        {
            return;
        }

        _source.pitch = Mathf.Lerp(0.65f, 1.45f, Mathf.Clamp01(progress));
    }

    public static void StopAntiMaterialCharge()
    {
        if (_source == null || !_antiMaterialChargePlaying)
        {
            return;
        }

        _source.Stop();
        _source.loop = false;
        _antiMaterialChargePlaying = false;
    }

    static AudioClip CreateAntiMaterialChargeClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.2f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float progress = t / duration;
            float envelope = Mathf.Clamp01(progress) * (1f - (progress * 0.15f));
            float tone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(90f, 240f, progress) * t);
            float rumble = Mathf.Sin(2f * Mathf.PI * 42f * t) * 0.35f;
            float noise = Random.Range(-1f, 1f) * 0.18f;
            samples[i] = (tone * 0.55f + rumble + noise) * envelope * 0.5f;
        }

        var clip = AudioClip.Create("AntiMaterialCharge", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static void PlayRangeDing(bool headshot)
    {
        EnsureInitialized();
        if (!MenuSettings.UiSoundsEnabled)
        {
            return;
        }

        float frequency = headshot ? 1560f : 980f;
        float volume = headshot ? 0.42f : 0.28f;
        _source.pitch = 1f;
        _source.PlayOneShot(CreateClickClip(frequency, 0.08f, volume));
    }

    public static void WireButton(Button button, bool playClick = true)
    {
        if (button == null)
        {
            return;
        }

        var trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        AddTrigger(trigger, EventTriggerType.PointerEnter, _ => PlayHover());
        if (playClick)
        {
            button.onClick.AddListener(PlayClick);
        }
    }

    static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    static AudioClip CreateClickClip(float frequency, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - (t / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
        }

        var clip = AudioClip.Create("UiClick", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip CreateGunshotClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.16f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-t * 28f);
            float noise = Random.Range(-1f, 1f);
            float tone = Mathf.Sin(2f * Mathf.PI * (180f - (t * 420f)) * t);
            samples[i] = (noise * 0.55f + tone * 0.45f) * envelope * 0.42f;
        }

        var clip = AudioClip.Create("MenuGunshot", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
