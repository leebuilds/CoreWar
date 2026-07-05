using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Stores profiles under Application.persistentDataPath (never committed to git).
/// </summary>
public class LocalProfileRepository : IProfileRepository
{
    readonly string _profilesDir;
    readonly string _indexPath;

    public LocalProfileRepository()
    {
        _profilesDir = Path.Combine(Application.persistentDataPath, "CoreWar", "profiles");
        _indexPath = Path.Combine(_profilesDir, "username_index.json");
        Directory.CreateDirectory(_profilesDir);
    }

    public bool UsernameExists(string username)
    {
        var index = LoadIndex();
        return index.ContainsKey(NormalizeUsername(username));
    }

    public bool TryCreateProfile(string username, string passcode, out PlayerProfile profile, out string error)
    {
        profile = null;
        error = null;

        var normalized = NormalizeUsername(username);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            error = "Username is required.";
            return false;
        }

        if (normalized.Length < 2)
        {
            error = "Username must be at least 2 characters.";
            return false;
        }

        if (string.IsNullOrEmpty(passcode) || passcode.Length < PasscodeUtility.MinPasscodeLength)
        {
            error = $"Passcode must be at least {PasscodeUtility.MinPasscodeLength} characters.";
            return false;
        }

        if (UsernameExists(normalized))
        {
            error = "Username is already taken.";
            return false;
        }

        var salt = PasscodeUtility.GenerateSalt();
        var hash = PasscodeUtility.HashPasscode(passcode, salt);
        profile = PlayerProfile.CreateNew(normalized, hash, salt, CardCatalog.AllCardIds());
        SaveProfile(profile);

        var index = LoadIndex();
        index[normalized] = profile.profileId;
        SaveIndex(index);
        return true;
    }

    public bool TrySignIn(string username, string passcode, out PlayerProfile profile, out string error)
    {
        profile = null;
        error = null;

        var normalized = NormalizeUsername(username);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            error = "Username is required.";
            return false;
        }

        var index = LoadIndex();
        if (!index.TryGetValue(normalized, out var profileId))
        {
            error = "Account not found.";
            return false;
        }

        profile = LoadProfile(profileId);
        if (profile == null)
        {
            error = "Account not found.";
            return false;
        }

        if (!PasscodeUtility.VerifyPasscode(passcode, profile.passcodeSalt, profile.passcodeHash))
        {
            profile = null;
            error = "Incorrect passcode.";
            return false;
        }

        return true;
    }

    public void SaveProfile(PlayerProfile profile)
    {
        if (profile == null || string.IsNullOrEmpty(profile.profileId))
        {
            return;
        }

        var path = ProfilePath(profile.profileId);
        File.WriteAllText(path, JsonUtility.ToJson(profile, prettyPrint: true));
    }

    public PlayerProfile LoadProfile(string profileId)
    {
        if (string.IsNullOrEmpty(profileId))
        {
            return null;
        }

        var path = ProfilePath(profileId);
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(path));
    }

    static string NormalizeUsername(string username)
    {
        return username == null ? string.Empty : username.Trim().ToLowerInvariant();
    }

    string ProfilePath(string profileId)
    {
        return Path.Combine(_profilesDir, profileId + ".json");
    }

    Dictionary<string, string> LoadIndex()
    {
        if (!File.Exists(_indexPath))
        {
            return new Dictionary<string, string>();
        }

        var wrapper = JsonUtility.FromJson<UsernameIndexWrapper>(File.ReadAllText(_indexPath));
        var map = new Dictionary<string, string>();
        if (wrapper?.entries == null)
        {
            return map;
        }

        foreach (var entry in wrapper.entries)
        {
            if (!string.IsNullOrEmpty(entry.username) && !string.IsNullOrEmpty(entry.profileId))
            {
                map[entry.username] = entry.profileId;
            }
        }

        return map;
    }

    void SaveIndex(Dictionary<string, string> index)
    {
        var wrapper = new UsernameIndexWrapper { entries = new UsernameIndexEntry[index.Count] };
        int i = 0;
        foreach (var pair in index)
        {
            wrapper.entries[i++] = new UsernameIndexEntry
            {
                username = pair.Key,
                profileId = pair.Value
            };
        }

        File.WriteAllText(_indexPath, JsonUtility.ToJson(wrapper, prettyPrint: true));
    }

    [Serializable]
    class UsernameIndexWrapper
    {
        public UsernameIndexEntry[] entries;
    }

    [Serializable]
    class UsernameIndexEntry
    {
        public string username;
        public string profileId;
    }
}
