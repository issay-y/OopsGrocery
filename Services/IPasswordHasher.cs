namespace OopsGrocery.Services;

public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a raw password into a cryptographically secure PBKDF2 string.
    /// Format: PBKDF2$iterations$base64Salt$base64Hash
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a raw password against a stored PBKDF2 hash using constant-time comparison.
    /// </summary>
    bool VerifyPassword(string password, string storedHash);
}
