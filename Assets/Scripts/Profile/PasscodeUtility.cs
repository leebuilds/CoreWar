using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Local passcode hashing. Replace with server-side auth when going online.
/// </summary>
public static class PasscodeUtility
{
    public const int MinPasscodeLength = 4;

    public static string GenerateSalt()
    {
        var bytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        return Convert.ToBase64String(bytes);
    }

    public static string HashPasscode(string passcode, string salt)
    {
        using (var sha = SHA256.Create())
        {
            var payload = Encoding.UTF8.GetBytes(passcode + salt);
            var hash = sha.ComputeHash(payload);
            return Convert.ToBase64String(hash);
        }
    }

    public static bool VerifyPasscode(string passcode, string salt, string expectedHash)
    {
        if (string.IsNullOrEmpty(passcode) || string.IsNullOrEmpty(salt) || string.IsNullOrEmpty(expectedHash))
        {
            return false;
        }

        return HashPasscode(passcode, salt) == expectedHash;
    }
}
