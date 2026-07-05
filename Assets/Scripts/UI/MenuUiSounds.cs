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
        EnsureInitialized();
        _source.pitch = Random.Range(0.92f, 1.02f);
        if (_gunshotClip == null)
        {
            _gunshotClip = CreateGunshotClip();
        }

        _source.PlayOneShot(_gunshotClip);
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
