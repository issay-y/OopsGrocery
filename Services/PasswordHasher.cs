using System.Security.Cryptography;

namespace OopsGrocery.Services;

/// <summary>
/// Provides secure password hashing and verification using PBKDF2 with HMAC-SHA256.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;       // 128-bit salt
    private const int SubkeySizeBytes = 32;     // 256-bit hash
    private const int IterationCount = 100_000;  // Minimum iterations for PBKDF2
    private const string FormatPrefix = "PBKDF2";
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static PasswordHasher Default { get; } = new();

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            IterationCount,
            Algorithm,
            SubkeySizeBytes);

        return $"{FormatPrefix}${IterationCount}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != FormatPrefix ||
            !int.TryParse(parts[1], out var iterations) || iterations < 100_000)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                Algorithm,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
