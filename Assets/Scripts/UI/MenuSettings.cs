using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Persistent client settings shared by menus and gameplay.
/// </summary>
[Serializable]
public class MenuSettingsData
{
    public bool darkMode;
    public float masterVolume = 0.55f;
    public bool uiSoundsEnabled = true;
    public float mouseSensitivity = 1f;
}

public static class MenuSettings
{
    static MenuSettingsData _data = new MenuSettingsData();
    static bool _loaded;

    public static event Action Changed;

    public static bool IsDarkMode => _data.darkMode;
    public static float MasterVolume => Mathf.Clamp01(_data.masterVolume);
    public static bool UiSoundsEnabled => _data.uiSoundsEnabled;
    public static float MouseSensitivity => Mathf.Clamp(_data.mouseSensitivity, 0.25f, 2.5f);

    public static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        string path = SettingsPath();
        if (!File.Exists(path))
        {
            ApplySideEffects();
            return;
        }

        try
        {
            _data = JsonUtility.FromJson<MenuSettingsData>(File.ReadAllText(path)) ?? new MenuSettingsData();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load menu settings: {exception.Message}");
            _data = new MenuSettingsData();
        }

        ApplySideEffects();
    }

    public static void SetDarkMode(bool darkMode)
    {
        EnsureLoaded();
        if (_data.darkMode == darkMode)
        {
            return;
        }

        _data.darkMode = darkMode;
        SaveAndNotify(notify: true);
    }

    public static void SetMasterVolume(float volume, bool notify = false)
    {
        EnsureLoaded();
        _data.masterVolume = Mathf.Clamp01(volume);
        SaveAndNotify(notify);
    }

    public static void SetUiSoundsEnabled(bool enabled, bool notify = false)
    {
        EnsureLoaded();
        if (_data.uiSoundsEnabled == enabled)
        {
            return;
        }

        _data.uiSoundsEnabled = enabled;
        SaveAndNotify(notify);
    }

    public static void SetMouseSensitivity(float sensitivity, bool notify = false)
    {
        EnsureLoaded();
        _data.mouseSensitivity = Mathf.Clamp(sensitivity, 0.25f, 2.5f);
        SaveAndNotify(notify);
    }

    static void SaveAndNotify(bool notify)
    {
        Save();
        ApplySideEffects();
        if (notify)
        {
            Changed?.Invoke();
        }
    }

    static void Save()
    {
        string path = SettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(_data, prettyPrint: true));
    }

    static string SettingsPath()
    {
        return Path.Combine(Application.persistentDataPath, "CoreWar", "settings.json");
    }

    static void ApplySideEffects()
    {
        MenuUiSounds.ApplySettings();
    }
}
