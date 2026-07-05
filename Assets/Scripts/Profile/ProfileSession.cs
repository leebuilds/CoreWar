using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Tracks the active signed-in profile and enforces offline session expiry.
/// </summary>
public static class ProfileSession
{
    public const double SessionTimeoutHours = 1.0;

    static IProfileRepository _repository;
    static PlayerProfile _activeProfile;
    static bool _initialized;

    public static IProfileRepository Repository
    {
        get
        {
            EnsureInitialized();
            return _repository;
        }
    }

    public static PlayerProfile ActiveProfile => _activeProfile;
    public static bool IsSignedIn => _activeProfile != null;

    public static bool HasCompleteLoadout => _activeProfile != null && _activeProfile.HasCompleteLoadout;

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            ValidateSessionOrLogout();
            return;
        }

        _repository = new LocalProfileRepository();
        TryRestoreSession();
        _initialized = true;
    }

    public static void ValidateSessionOrLogout()
    {
        if (_activeProfile == null)
        {
            return;
        }

        if (IsSessionExpired(_activeProfile))
        {
            Logout();
        }
    }

    public static void SignIn(PlayerProfile profile)
    {
        EnsureInitialized();
        _activeProfile = profile;
        profile.TouchLastActive();
        _repository.SaveProfile(profile);
        SaveSessionFile(profile.profileId);
    }

    public static void Logout()
    {
        _activeProfile = null;
        DeleteSessionFile();
    }

    public static void TouchActivity()
    {
        if (_activeProfile == null)
        {
            return;
        }

        _activeProfile.TouchLastActive();
        _repository.SaveProfile(_activeProfile);
        SaveSessionFile(_activeProfile.profileId);
    }

    public static void SaveActiveProfile()
    {
        if (_activeProfile == null)
        {
            return;
        }

        _repository.SaveProfile(_activeProfile);
    }

    public static bool OwnsCard(string cardId)
    {
        if (_activeProfile?.ownedCardIds == null || string.IsNullOrEmpty(cardId))
        {
            return false;
        }

        foreach (var owned in _activeProfile.ownedCardIds)
        {
            if (owned == cardId)
            {
                return true;
            }
        }

        return false;
    }

    public static void SetLoadoutSlot(int slotIndex, string cardId)
    {
        if (_activeProfile == null || slotIndex < 0 || slotIndex > 1)
        {
            return;
        }

        if (_activeProfile.loadoutCardIds == null || _activeProfile.loadoutCardIds.Length < 2)
        {
            _activeProfile.loadoutCardIds = new[] { string.Empty, string.Empty };
        }

        _activeProfile.loadoutCardIds[slotIndex] = cardId ?? string.Empty;
        SaveActiveProfile();
    }

    public static void AddToLoadout(string cardId)
    {
        if (_activeProfile == null || string.IsNullOrEmpty(cardId))
        {
            return;
        }

        if (_activeProfile.loadoutCardIds == null || _activeProfile.loadoutCardIds.Length < 2)
        {
            _activeProfile.loadoutCardIds = new[] { string.Empty, string.Empty };
        }

        if (string.IsNullOrEmpty(_activeProfile.loadoutCardIds[0]))
        {
            _activeProfile.loadoutCardIds[0] = cardId;
        }
        else if (string.IsNullOrEmpty(_activeProfile.loadoutCardIds[1]))
        {
            _activeProfile.loadoutCardIds[1] = cardId;
        }
        else
        {
            _activeProfile.loadoutCardIds[1] = cardId;
        }

        SaveActiveProfile();
    }

    public static void ClearLoadoutSlot(int slotIndex)
    {
        SetLoadoutSlot(slotIndex, string.Empty);
    }

    static void TryRestoreSession()
    {
        var sessionPath = SessionFilePath();
        if (!File.Exists(sessionPath))
        {
            return;
        }

        var wrapper = JsonUtility.FromJson<SessionFile>(File.ReadAllText(sessionPath));
        if (wrapper == null || string.IsNullOrEmpty(wrapper.profileId))
        {
            DeleteSessionFile();
            return;
        }

        var profile = _repository.LoadProfile(wrapper.profileId);
        if (profile == null)
        {
            DeleteSessionFile();
            return;
        }

        if (IsSessionExpired(profile))
        {
            DeleteSessionFile();
            return;
        }

        _activeProfile = profile;
    }

    static bool IsSessionExpired(PlayerProfile profile)
    {
        if (profile == null)
        {
            return true;
        }

        if (string.IsNullOrEmpty(profile.lastActiveUtc))
        {
            return false;
        }

        var lastActive = profile.GetLastActiveUtc();
        var elapsed = DateTime.UtcNow - lastActive;
        return elapsed.TotalHours > SessionTimeoutHours;
    }

    static void SaveSessionFile(string profileId)
    {
        var wrapper = new SessionFile { profileId = profileId };
        File.WriteAllText(SessionFilePath(), JsonUtility.ToJson(wrapper));
    }

    static void DeleteSessionFile()
    {
        var path = SessionFilePath();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    static string SessionFilePath()
    {
        var dir = Path.Combine(Application.persistentDataPath, "CoreWar");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "session.json");
    }

    [Serializable]
    class SessionFile
    {
        public string profileId;
    }
}
