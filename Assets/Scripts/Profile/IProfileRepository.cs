/// <summary>
/// Profile persistence boundary. LocalProfileRepository today; OnlineProfileRepository later.
/// </summary>
public interface IProfileRepository
{
    bool UsernameExists(string username);
    bool TryCreateProfile(string username, string passcode, out PlayerProfile profile, out string error);
    bool TrySignIn(string username, string passcode, out PlayerProfile profile, out string error);
    void SaveProfile(PlayerProfile profile);
    PlayerProfile LoadProfile(string profileId);
}
