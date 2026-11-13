using System.Security.Cryptography;

namespace AccessHub.BuildingBlocks.Application;

/// <summary>
/// Site API keys: 32 random bytes shown once (base64url), stored only as a SHA-256 hash,
/// and compared in constant time.
/// </summary>
public static class SiteApiKey
{
    private const int KeyLength = 32;

    /// <summary>Generates a new key. The plaintext must be shown to the caller once and never stored.</summary>
    public static (string Plaintext, byte[] Hash) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(KeyLength);
        return (Base64UrlEncode(bytes), SHA256.HashData(bytes));
    }

    /// <summary>Hashes a presented key; returns null if it is not a well-formed key.</summary>
    public static byte[]? TryHash(string presented)
    {
        var bytes = TryBase64UrlDecode(presented);
        return bytes is { Length: KeyLength } ? SHA256.HashData(bytes) : null;
    }

    /// <summary>Checks a presented key against a stored hash in constant time.</summary>
    public static bool Verify(string presented, ReadOnlySpan<byte> storedHash)
    {
        var hash = TryHash(presented);
        return hash is not null && CryptographicOperations.FixedTimeEquals(hash, storedHash);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[]? TryBase64UrlDecode(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
        {
            return null;
        }

        var s = value.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + ((4 - (s.Length % 4)) % 4), '=');
        var buffer = new byte[s.Length];
        return Convert.TryFromBase64String(s, buffer, out var written) ? buffer[..written] : null;
    }
}
