using System.Security.Cryptography;
using System.Text;

namespace RequiemNexus.Application.Security;

/// <summary>
/// Generates and verifies high-entropy campaign join invite tokens. Stores only SHA-256 hashes (64 hex chars).
/// </summary>
public static class CampaignInviteTokenHasher
{
    /// <summary>Creates a new URL-safe random token (32 bytes, base64url).</summary>
    public static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return ToBase64Url(bytes);
    }

    /// <summary>Computes the lowercase hex SHA-256 hash of <paramref name="token"/> for persistence.</summary>
    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="presentedToken"/> hashes to <paramref name="storedHashHex"/>.
    /// </summary>
    public static bool Verify(string? storedHashHex, string? presentedToken)
    {
        if (string.IsNullOrEmpty(storedHashHex) || string.IsNullOrEmpty(presentedToken))
        {
            return false;
        }

        byte[] stored;
        try
        {
            stored = Convert.FromHexString(storedHashHex);
        }
        catch (FormatException)
        {
            return false;
        }

        if (stored.Length != 32)
        {
            return false;
        }

        byte[] computed = SHA256.HashData(Encoding.UTF8.GetBytes(presentedToken));
        return CryptographicOperations.FixedTimeEquals(stored, computed);
    }

    private static string ToBase64Url(ReadOnlySpan<byte> data)
    {
        string b64 = Convert.ToBase64String(data);
        return b64.TrimEnd('=').Replace("+", "-", StringComparison.Ordinal).Replace("/", "_", StringComparison.Ordinal);
    }
}
